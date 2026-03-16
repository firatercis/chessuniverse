using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum BluffyPhase
{
    Setup,
    PassDevice,
    Playing,
    WaitingBluff,
    Sacrifice,
    Rearrange,
    GameOver
}

public class BluffyManager : MonoBehaviour
{
    public static BluffyManager Instance { get; private set; }

    public BluffyPhase currentPhase;

    // Real types behind masks
    public Dictionary<ChessPiece, PieceType> realTypes = new();
    // Mask color index per piece (0-7)
    public Dictionary<ChessPiece, int> maskIndices = new();

    // Setup state
    private PieceColor setupColor;
    private ChessPiece selectedSetupPiece;
    private bool setupComplete;

    // Pending move (for bluff resolution) - exposed for BluffyAI
    private ChessPiece pendingMovePiece;
    private Vector2Int pendingFrom;
    private Vector2Int pendingTo;
    private ChessPiece capturedByPending;
    private bool pendingHadMoved;
    private int capturedMaskIndex = -1;
    private PieceType capturedRealType;

    // Public accessors for BluffyAI
    public ChessPiece PendingMovePiece => pendingMovePiece;
    public Vector2Int PendingFrom => pendingFrom;
    public Vector2Int PendingTo => pendingTo;
    public ChessPiece CapturedByPending => capturedByPending;

    // Swap selection for rearrange
    private ChessPiece selectedSwapPiece;

    // Which color must sacrifice / rearrange
    private PieceColor sacrificeColor;
    private PieceColor rearrangeColor;

    // Pass device target
    private PieceColor passDeviceTarget;

    // Mask sprite loaded from Resources
    private Sprite maskSprite;

    // Settings reference
    private BluffySettings settings;

    // Peek state
    private bool isPeeking;

    // Was last move by a big piece (bluff eligible)
    public bool lastMoveWasBigPiece;

    // Who made the pending move
    private PieceColor pendingMoveColor;

    // After pass device, go to rearrange instead of playing
    private bool pendingRearrangeAfterPass;

    static readonly Color[] MaskColors = {
        new(0.9f, 0.2f, 0.2f), // Red
        new(0.2f, 0.5f, 0.9f), // Blue
        new(0.2f, 0.8f, 0.3f), // Green
        new(0.9f, 0.8f, 0.1f), // Yellow
        new(0.7f, 0.3f, 0.9f), // Purple
        new(0.9f, 0.5f, 0.1f), // Orange
        new(0.1f, 0.8f, 0.8f), // Cyan
        new(0.9f, 0.4f, 0.7f), // Pink
    };

    // The piece that was just moved (for targeted rearrange)
    private ChessPiece movedPiece;
    private bool rearrangeSwapDone;

    private bool isSinglePlayer => GameManager.Instance.currentPlayMode == PlayMode.SinglePlayer;
    private bool isOnline => GameManager.Instance.currentPlayMode == PlayMode.Online;

    private void Awake()
    {
        Instance = this;
        maskSprite = Resources.Load<Sprite>("ChessPieces/mask_filled");
        settings = Resources.Load<BluffySettings>("BluffySettings");
        if (settings == null)
            settings = ScriptableObject.CreateInstance<BluffySettings>();
    }

    private void Update()
    {
        if (GameBootstrap.CurrentMode != GameMode.BluffyChess) return;
        if (currentPhase == BluffyPhase.GameOver) return;

        // Debug peek: hold key to see real pieces behind masks (desktop only)
#if !UNITY_IOS && !UNITY_ANDROID && !UNITY_WEBGL
        if (Input.GetKeyDown(settings.peekKey) && !isPeeking)
        {
            isPeeking = true;
            ShowAllReal();
        }
        else if (Input.GetKeyUp(settings.peekKey) && isPeeking)
        {
            isPeeking = false;
            PieceColor viewer;
            if (isOnline)
                viewer = NetworkManager.Instance.MyColor;
            else if (isSinglePlayer)
                viewer = PieceColor.White;
            else
                viewer = GameManager.Instance.currentTurn;
            RefreshPerspective(viewer);
        }
#endif
    }

    private void ShowAllReal()
    {
        var board = ChessBoard.Instance.board;
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece p = board[x, y];
                if (p == null) continue;
                p.HideMask();
            }
        }
    }

    public void StartSetupPhase()
    {
        realTypes.Clear();
        maskIndices.Clear();
        selectedSetupPiece = null;
        setupComplete = false;

        // Register all pieces with their default types and assign mask indices
        var board = ChessBoard.Instance.board;
        int whiteIdx = 0;
        int blackIdx = 0;

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece p = board[x, y];
                if (p == null) continue;

                realTypes[p] = p.type;

                if (p.type != PieceType.Pawn)
                {
                    if (p.color == PieceColor.White)
                        maskIndices[p] = whiteIdx++;
                    else
                        maskIndices[p] = blackIdx++;
                }
            }
        }

        // Online: each player sets up their own color
        if (isOnline)
        {
            setupColor = NetworkManager.Instance.MyColor;
            currentPhase = BluffyPhase.Setup;
            RefreshPerspective(NetworkManager.Instance.MyColor);
            UIManager.Instance.ShowSetupPanel(setupColor);
            return;
        }

        // Start white setup
        setupColor = PieceColor.White;
        currentPhase = BluffyPhase.Setup;
        RefreshPerspective(PieceColor.White);
        UIManager.Instance.ShowSetupPanel(PieceColor.White);
    }

    public void HandleSetupClick(Vector2Int pos)
    {
        var board = ChessBoard.Instance.board;
        ChessPiece clicked = board[pos.x, pos.y];

        if (clicked == null) return;
        if (clicked.color != setupColor) return;
        if (clicked.type == PieceType.Pawn) return; // Can't swap pawns

        if (selectedSetupPiece == null)
        {
            selectedSetupPiece = clicked;
            ChessBoard.Instance.HighlightSquare(pos.x, pos.y, ChessBoard.Instance.selectedColor);
        }
        else
        {
            if (clicked == selectedSetupPiece)
            {
                // Deselect
                selectedSetupPiece = null;
                ChessBoard.Instance.ClearHighlights();
                return;
            }

            // Swap the two pieces
            SwapPiecePositions(selectedSetupPiece, clicked);
            selectedSetupPiece = null;
            ChessBoard.Instance.ClearHighlights();
        }
    }

    public void ConfirmSetup()
    {
        selectedSetupPiece = null;
        ChessBoard.Instance.ClearHighlights();

        if (isOnline)
        {
            // Online: serialize my back rank positions and send
            UIManager.Instance.HideSetupPanel();
            string positionsJson = SerializeMyBackRank();
            GameLogger.Instance?.LogBluffySetup(NetworkManager.Instance.MyColor, positionsJson);
            NetworkManager.Instance.SendMySetup(positionsJson);
            // Show waiting message until opponent setup arrives
            UIManager.Instance.ShowInfoPanel("Waiting for opponent setup...");
            return;
        }

        if (setupColor == PieceColor.White)
        {
            if (isSinglePlayer)
            {
                UIManager.Instance.HideSetupPanel();
                GameLogger.Instance?.LogBluffySetup(PieceColor.White, SerializeBackRank(PieceColor.White));
                BluffyAI.Instance.ShuffleBackRank(ChessBoard.Instance.board);
                GameLogger.Instance?.LogBluffySetup(PieceColor.Black, SerializeBackRank(PieceColor.Black));
                RandomizeMaskIndices();
                BluffyAI.Instance.InitBeliefs();
                setupComplete = true;
                currentPhase = BluffyPhase.Playing;
                GameManager.Instance.currentTurn = PieceColor.White;
                RefreshPerspective(PieceColor.White);
                UIManager.Instance.UpdateBluffyTurnText(PieceColor.White);
            }
            else
            {
                GameLogger.Instance?.LogBluffySetup(PieceColor.White, SerializeBackRank(PieceColor.White));
                setupColor = PieceColor.Black;
                passDeviceTarget = PieceColor.Black;
                currentPhase = BluffyPhase.PassDevice;
                UIManager.Instance.HideSetupPanel();
                UIManager.Instance.ShowPassDevicePanel(PieceColor.Black);
            }
        }
        else
        {
            GameLogger.Instance?.LogBluffySetup(PieceColor.Black, SerializeBackRank(PieceColor.Black));
            passDeviceTarget = PieceColor.White;
            currentPhase = BluffyPhase.PassDevice;
            UIManager.Instance.HideSetupPanel();
            UIManager.Instance.ShowPassDevicePanel(PieceColor.White);
        }
    }

    public void OnPassDeviceReady()
    {
        UIManager.Instance.HidePassDevicePanel();

        if (currentPhase == BluffyPhase.PassDevice)
        {
            // Still in setup phase?
            if (!setupComplete)
            {
                if (setupColor == PieceColor.Black && passDeviceTarget == PieceColor.Black)
                {
                    // Black's setup turn
                    currentPhase = BluffyPhase.Setup;
                    RefreshPerspective(PieceColor.Black);
                    UIManager.Instance.ShowSetupPanel(PieceColor.Black);
                    return;
                }

                // Both done, randomize masks so king can't be tracked, start playing as White
                RandomizeMaskIndices();
                setupComplete = true;
                currentPhase = BluffyPhase.Playing;
                GameManager.Instance.currentTurn = PieceColor.White;
                RefreshPerspective(PieceColor.White);
                UIManager.Instance.UpdateBluffyTurnText(PieceColor.White);
                return;
            }

            // After sacrifice: go to rearrange
            if (pendingRearrangeAfterPass)
            {
                pendingRearrangeAfterPass = false;
                currentPhase = BluffyPhase.Rearrange;
                selectedSwapPiece = null;
                RefreshPerspective(rearrangeColor);
                // Highlight the moved piece so user knows which to click
                if (movedPiece != null)
                    ChessBoard.Instance.HighlightSquare(movedPiece.x, movedPiece.y, ChessBoard.Instance.selectedColor);
                UIManager.Instance.ShowRearrangePanel(rearrangeColor);
                return;
            }

            // Mid-game pass device
            PieceColor viewer = passDeviceTarget;
            GameManager.Instance.currentTurn = viewer;
            RefreshPerspective(viewer);

            // Show bluff buttons if last move was a big piece
            if (lastMoveWasBigPiece)
            {
                currentPhase = BluffyPhase.WaitingBluff;
                UIManager.Instance.ShowBluffPanel();
            }
            else
            {
                currentPhase = BluffyPhase.Playing;
                UIManager.Instance.UpdateBluffyTurnText(viewer);
            }
        }
    }

    public void RefreshPerspective(PieceColor viewer)
    {
        var board = ChessBoard.Instance.board;
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece p = board[x, y];
                if (p == null) continue;

                if (p.isSeed)
                {
                    p.HideMask();
                }
                else if (p.color == viewer)
                {
                    // Own pieces: no mask
                    p.HideMask();
                }
                else if (p.type == PieceType.Pawn)
                {
                    p.HideMask();
                }
                else
                {
                    // Opponent's big pieces: full mask hiding the piece
                    if (maskIndices.ContainsKey(p))
                        p.ShowMask(maskSprite, MaskColors[maskIndices[p]]);
                }
            }
        }

        if (isSinglePlayer)
        {
            ChessBoard.Instance.SetFlipped(false);
        }
        else if (isOnline)
        {
            // Online: each player always sees their own perspective
            ChessBoard.Instance.SetFlipped(!NetworkManager.Instance.IsHost);
        }
        else
        {
            // Local: flip the board so the current viewer's side is at the bottom
            ChessBoard.Instance.SetFlipped(viewer == PieceColor.Black);
        }
    }

    public void StorePendingMove(ChessPiece piece, Vector2Int from, Vector2Int to, ChessPiece captured, bool hadMoved)
    {
        pendingMovePiece = piece;
        pendingFrom = from;
        pendingTo = to;
        capturedByPending = captured;
        pendingHadMoved = hadMoved;
        pendingMoveColor = piece.color;
        movedPiece = piece;

        // Save captured piece's original registration data for undo
        if (captured != null)
        {
            capturedRealType = realTypes.ContainsKey(captured) ? realTypes[captured] : captured.type;
            capturedMaskIndex = maskIndices.ContainsKey(captured) ? maskIndices[captured] : -1;
        }
        else
        {
            capturedMaskIndex = -1;
        }
    }

    public bool ValidateBluff()
    {
        // Check if the pending move would be legal with the piece's REAL type
        // using standard chess moves (not bluffy superpiece moves)
        PieceType trueType = realTypes[pendingMovePiece];

        PieceType savedType = pendingMovePiece.type;
        pendingMovePiece.type = trueType;

        var board = ChessBoard.Instance.board;
        int curX = pendingMovePiece.x;
        int curY = pendingMovePiece.y;

        // The piece is currently at pendingTo, move it back to pendingFrom for check
        board[pendingTo.x, pendingTo.y] = capturedByPending;
        board[pendingFrom.x, pendingFrom.y] = pendingMovePiece;
        pendingMovePiece.x = pendingFrom.x;
        pendingMovePiece.y = pendingFrom.y;

        if (capturedByPending != null)
            capturedByPending.gameObject.SetActive(true);

        // Temporarily switch to Classic mode so GetRawMoves returns standard moves
        var savedMode = GameBootstrap.CurrentMode;
        GameBootstrap.CurrentMode = GameMode.Classic;

        var rawMoves = pendingMovePiece.GetRawMoves(board);
        bool isLegal = rawMoves.Contains(pendingTo);

        GameBootstrap.CurrentMode = savedMode;

        // Restore board state
        if (capturedByPending != null)
            capturedByPending.gameObject.SetActive(false);

        board[pendingFrom.x, pendingFrom.y] = null;
        board[pendingTo.x, pendingTo.y] = pendingMovePiece;
        pendingMovePiece.x = curX;
        pendingMovePiece.y = curY;

        pendingMovePiece.type = savedType;

        return isLegal; // true = legal (bluff caller was wrong), false = bluff caught
    }

    public void ResolveCaughtBluffing()
    {
        // The move was illegal with real type - piece dies, move undone
        // Re-register the captured piece with its original data (was unregistered in ExecuteBluffyMove)
        if (capturedByPending != null && !realTypes.ContainsKey(capturedByPending))
        {
            realTypes[capturedByPending] = capturedRealType;
            if (capturedByPending.type != PieceType.Pawn && capturedMaskIndex >= 0)
            {
                maskIndices[capturedByPending] = capturedMaskIndex;
            }
        }

        GameLogger.Instance?.LogBluffCaught(pendingFrom.x, pendingFrom.y, pendingTo.x, pendingTo.y,
            capturedByPending != null ? capturedRealType : (PieceType?)null);

        UndoPendingMove();

        ChessPiece caughtPiece = pendingMovePiece;
        // Update beliefs before destroying
        if (isSinglePlayer)
            BluffyAI.Instance.UpdateBeliefsOnLoss(caughtPiece);

        UnregisterPiece(caughtPiece);
        ChessBoard.Instance.board[caughtPiece.x, caughtPiece.y] = null;
        Object.Destroy(caughtPiece.gameObject);

        capturedByPending = null;

        UIManager.Instance.HideBluffPanel();

        // Turn stays with the mover - they get another chance
        PieceColor mover = pendingMoveColor;
        ClearPendingMove();

        if (isOnline)
        {
            UIManager.Instance.ShowInfoPanel("Bluff caught!\nPiece removed.", () =>
            {
                lastMoveWasBigPiece = false;
                GameManager.Instance.currentTurn = mover;
                currentPhase = BluffyPhase.Playing;
                RefreshPerspective(NetworkManager.Instance.MyColor);
                UIManager.Instance.UpdateBluffyTurnText(mover);
            });
        }
        else if (isSinglePlayer)
        {
            UIManager.Instance.ShowInfoPanel("Bluff caught!\nPiece removed.", () =>
            {
                lastMoveWasBigPiece = false;
                GameManager.Instance.currentTurn = mover;
                currentPhase = BluffyPhase.Playing;
                RefreshPerspective(PieceColor.White);

                if (mover == PieceColor.Black)
                {
                    TriggerAITurn();
                }
                else
                {
                    UIManager.Instance.UpdateBluffyTurnText(PieceColor.White);
                }
            });
        }
        else
        {
            UIManager.Instance.ShowInfoPanel("Bluff caught!\nPiece removed.", () =>
            {
                passDeviceTarget = mover;
                lastMoveWasBigPiece = false;
                currentPhase = BluffyPhase.PassDevice;
                GameManager.Instance.currentTurn = mover;
                ShowPassDeviceDelayed(mover);
            });
        }
    }

    public void ResolveSuccessfulDefense()
    {
        // The move was legal - bluff caller was wrong
        // Destroy the captured piece permanently if any
        if (capturedByPending != null)
        {
            if (isSinglePlayer)
                BluffyAI.Instance.UpdateBeliefsOnLoss(capturedByPending);
            Object.Destroy(capturedByPending.gameObject);
            capturedByPending = null;
        }

        // Check if king was captured
        if (IsKingCaptured(out PieceColor winner))
        {
            currentPhase = BluffyPhase.GameOver;
            GameLogger.Instance?.EndGame($"{winner.ToString().ToLower()}_wins");
            UIManager.Instance.HideBluffPanel();
            UIManager.Instance.ShowBluffyGameOver(winner);
            return;
        }

        UIManager.Instance.HideBluffPanel();

        // Bluff caller must sacrifice a big piece
        PieceColor callerColor = pendingMoveColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
        sacrificeColor = callerColor;
        rearrangeColor = pendingMoveColor;

        if (isOnline)
        {
            if (callerColor == NetworkManager.Instance.MyColor)
            {
                // We were wrong - we must sacrifice
                UIManager.Instance.ShowInfoPanel("Not a bluff!\nYou must sacrifice a big piece.", () =>
                {
                    currentPhase = BluffyPhase.Sacrifice;
                    RefreshPerspective(NetworkManager.Instance.MyColor);
                    UIManager.Instance.ShowSacrificePanel(callerColor);
                });
            }
            else
            {
                // Opponent was wrong - wait for their sacrifice
                UIManager.Instance.ShowInfoPanel("Not a bluff!\nOpponent must sacrifice a big piece.", () =>
                {
                    currentPhase = BluffyPhase.Sacrifice;
                    UIManager.Instance.UpdateBluffyTurnText(callerColor);
                });
            }
        }
        else if (isSinglePlayer && callerColor == PieceColor.Black)
        {
            UIManager.Instance.ShowInfoPanel("Not a bluff!\nAI must sacrifice a big piece.", () =>
            {
                AutoAISacrificeAndSwap();
            });
        }
        else if (isSinglePlayer && callerColor == PieceColor.White)
        {
            UIManager.Instance.ShowInfoPanel("Not a bluff!\nYou must sacrifice a big piece.", () =>
            {
                currentPhase = BluffyPhase.Sacrifice;
                RefreshPerspective(PieceColor.White);
                UIManager.Instance.ShowSacrificePanel(callerColor);
            });
        }
        else
        {
            UIManager.Instance.ShowInfoPanel("Not a bluff!\nYou must sacrifice a big piece.", () =>
            {
                currentPhase = BluffyPhase.Sacrifice;
                UIManager.Instance.ShowSacrificePanel(callerColor);
            });
        }
    }

    private void AutoAISacrificeAndSwap()
    {
        var board = ChessBoard.Instance.board;

        // AI auto-sacrifice
        ChessPiece sacrifice = BluffyAI.Instance.ChooseSacrifice(PieceColor.Black);
        if (sacrifice != null)
        {
            GameLogger.Instance?.LogSacrifice(sacrifice.x, sacrifice.y);
            UnregisterPiece(sacrifice);
            board[sacrifice.x, sacrifice.y] = null;
            Object.Destroy(sacrifice.gameObject);
        }

        // Rearrange belongs to the mover, not the caller
        if (rearrangeColor == PieceColor.White)
        {
            // Human rearranges their moved piece
            currentPhase = BluffyPhase.Rearrange;
            selectedSwapPiece = null;
            RefreshPerspective(PieceColor.White);
            if (movedPiece != null)
                ChessBoard.Instance.HighlightSquare(movedPiece.x, movedPiece.y, ChessBoard.Instance.selectedColor);
            UIManager.Instance.ShowRearrangePanel(rearrangeColor);
        }
        else
        {
            // AI was the mover → auto-swap
            if (movedPiece != null)
            {
                ChessPiece swapTarget = BluffyAI.Instance.ChooseSwapTarget(movedPiece);
                if (swapTarget != null)
                {
                    GameLogger.Instance?.LogRearrangeSwap(movedPiece.x, movedPiece.y, swapTarget.x, swapTarget.y);
                    SwapPiecePositionsKeepMasks(movedPiece, swapTarget);
                }
            }
            GameLogger.Instance?.LogRearrangeDone();
            FinishRearrangeSP();
        }
    }

    public void HandleSacrificeClick(Vector2Int pos)
    {
        var board = ChessBoard.Instance.board;
        ChessPiece clicked = board[pos.x, pos.y];

        if (clicked == null) return;
        if (clicked.color != sacrificeColor) return;
        if (clicked.type == PieceType.Pawn) return;

        // Online: push sacrifice
        if (isOnline)
            NetworkManager.Instance.PushAction(NetworkAction.Sacrifice(pos.x, pos.y));

        GameLogger.Instance?.LogSacrifice(pos.x, pos.y);

        // Sacrifice this piece
        UnregisterPiece(clicked);
        board[pos.x, pos.y] = null;
        Object.Destroy(clicked.gameObject);

        UIManager.Instance.HideSacrificePanel();

        if (isOnline)
        {
            // Rearrange goes to the mover
            if (rearrangeColor == NetworkManager.Instance.MyColor)
            {
                currentPhase = BluffyPhase.Rearrange;
                selectedSwapPiece = null;
                RefreshPerspective(NetworkManager.Instance.MyColor);
                if (movedPiece != null)
                    ChessBoard.Instance.HighlightSquare(movedPiece.x, movedPiece.y, ChessBoard.Instance.selectedColor);
                UIManager.Instance.ShowRearrangePanel(rearrangeColor);
            }
            else
            {
                // Wait for remote rearrange
                currentPhase = BluffyPhase.Rearrange;
                UIManager.Instance.UpdateBluffyTurnText(rearrangeColor);
            }
        }
        else if (isSinglePlayer)
        {
            if (rearrangeColor == PieceColor.White)
            {
                currentPhase = BluffyPhase.Rearrange;
                selectedSwapPiece = null;
                RefreshPerspective(PieceColor.White);
                if (movedPiece != null)
                    ChessBoard.Instance.HighlightSquare(movedPiece.x, movedPiece.y, ChessBoard.Instance.selectedColor);
                UIManager.Instance.ShowRearrangePanel(rearrangeColor);
            }
            else
            {
                if (movedPiece != null)
                {
                    ChessPiece swapTarget = BluffyAI.Instance.ChooseSwapTarget(movedPiece);
                    if (swapTarget != null)
                    {
                        GameLogger.Instance?.LogRearrangeSwap(movedPiece.x, movedPiece.y, swapTarget.x, swapTarget.y);
                        SwapPiecePositionsKeepMasks(movedPiece, swapTarget);
                    }
                }
                GameLogger.Instance?.LogRearrangeDone();
                FinishRearrangeSP();
            }
        }
        else
        {
            pendingRearrangeAfterPass = true;
            passDeviceTarget = rearrangeColor;
            currentPhase = BluffyPhase.PassDevice;
            ShowPassDeviceDelayed(rearrangeColor);
        }
    }

    public void HandleRearrangeClick(Vector2Int pos)
    {
        var board = ChessBoard.Instance.board;
        ChessPiece clicked = board[pos.x, pos.y];

        if (clicked == null) return;
        if (clicked.color != rearrangeColor) return;
        if (clicked.type == PieceType.Pawn) return;

        if (selectedSwapPiece == null)
        {
            if (clicked != movedPiece) return;
            selectedSwapPiece = clicked;
            ChessBoard.Instance.HighlightSquare(pos.x, pos.y, ChessBoard.Instance.selectedColor);
        }
        else
        {
            if (clicked == selectedSwapPiece)
            {
                selectedSwapPiece = null;
                ChessBoard.Instance.ClearHighlights();
                return;
            }

            // Online: push rearrange swap
            if (isOnline)
                NetworkManager.Instance.PushAction(NetworkAction.RearrangeSwap(
                    selectedSwapPiece.x, selectedSwapPiece.y, clicked.x, clicked.y));

            GameLogger.Instance?.LogRearrangeSwap(selectedSwapPiece.x, selectedSwapPiece.y, clicked.x, clicked.y);

            // SP: AI loses track of swapped pieces — reset beliefs
            if (isSinglePlayer && rearrangeColor == PieceColor.White)
                BluffyAI.Instance.ResetBeliefsForSwap(selectedSwapPiece, clicked);

            SwapPiecePositionsKeepMasks(selectedSwapPiece, clicked);
            movedPiece = selectedSwapPiece;
            selectedSwapPiece = null;
            ChessBoard.Instance.ClearHighlights();

            rearrangeSwapDone = true;
            FinishRearrange();
        }
    }

    public void FinishRearrange()
    {
        // Online: push skip if no swap was performed (Done button clicked)
        if (isOnline && !rearrangeSwapDone && rearrangeColor == NetworkManager.Instance.MyColor)
            NetworkManager.Instance.PushAction(NetworkAction.RearrangeSkip());
        rearrangeSwapDone = false;

        GameLogger.Instance?.LogRearrangeDone();

        selectedSwapPiece = null;
        ChessBoard.Instance.ClearHighlights();
        UIManager.Instance.HideRearrangePanel();

        if (isOnline)
        {
            FinishRearrangeOnline();
        }
        else if (isSinglePlayer)
        {
            FinishRearrangeSP();
        }
        else
        {
            PieceColor opponent = pendingMoveColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
            ClearPendingMove();

            passDeviceTarget = opponent;
            lastMoveWasBigPiece = false;
            currentPhase = BluffyPhase.PassDevice;
            ShowPassDeviceDelayed(opponent);
        }
    }

    private void FinishRearrangeSP()
    {
        PieceColor opponent = pendingMoveColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
        ClearPendingMove();
        lastMoveWasBigPiece = false;
        currentPhase = BluffyPhase.Playing;
        GameManager.Instance.currentTurn = opponent;
        RefreshPerspective(PieceColor.White);

        if (opponent == PieceColor.Black)
        {
            TriggerAITurn();
        }
        else
        {
            UIManager.Instance.UpdateBluffyTurnText(PieceColor.White);
        }
    }

    public bool IsKingCaptured(out PieceColor winner)
    {
        bool whiteKingAlive = false;
        bool blackKingAlive = false;

        foreach (var kvp in realTypes)
        {
            if (kvp.Value == PieceType.King)
            {
                if (kvp.Key.color == PieceColor.White)
                    whiteKingAlive = true;
                else
                    blackKingAlive = true;
            }
        }

        if (!whiteKingAlive)
        {
            winner = PieceColor.Black;
            return true;
        }
        if (!blackKingAlive)
        {
            winner = PieceColor.White;
            return true;
        }

        winner = PieceColor.White;
        return false;
    }

    public void SwapPiecePositions(ChessPiece p1, ChessPiece p2)
    {
        var board = ChessBoard.Instance.board;

        int x1 = p1.x, y1 = p1.y;
        int x2 = p2.x, y2 = p2.y;

        board[x1, y1] = p2;
        board[x2, y2] = p1;

        p1.x = x2;
        p1.y = y2;
        p1.transform.position = ChessBoard.Instance.VisualPos(x2, y2);

        p2.x = x1;
        p2.y = y1;
        p2.transform.position = ChessBoard.Instance.VisualPos(x1, y1);
    }

    public void SwapPiecePositionsKeepMasks(ChessPiece p1, ChessPiece p2)
    {
        // Swap mask indices so colors stay at positions
        bool has1 = maskIndices.ContainsKey(p1);
        bool has2 = maskIndices.ContainsKey(p2);
        if (has1 && has2)
        {
            int idx1 = maskIndices[p1];
            int idx2 = maskIndices[p2];
            maskIndices[p1] = idx2;
            maskIndices[p2] = idx1;
        }

        // Swap physical positions
        SwapPiecePositions(p1, p2);
    }

    public void RandomizeMaskIndices()
    {
        // Randomize mask color assignments per color so king position can't be tracked
        RandomizeMaskIndicesForColor(PieceColor.White);
        RandomizeMaskIndicesForColor(PieceColor.Black);
    }

    private void RandomizeMaskIndicesForColor(PieceColor color)
    {
        var pieces = new List<ChessPiece>();
        foreach (var kvp in maskIndices)
        {
            if (kvp.Key.color == color)
                pieces.Add(kvp.Key);
        }

        if (pieces.Count == 0) return;

        // Fisher-Yates shuffle of indices
        var indices = pieces.Select(p => maskIndices[p]).ToList();
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        for (int i = 0; i < pieces.Count; i++)
        {
            maskIndices[pieces[i]] = indices[i];
        }
    }

    public void RegisterPiece(ChessPiece piece, PieceType realType, int maskIndex)
    {
        realTypes[piece] = realType;
        if (piece.type != PieceType.Pawn)
            maskIndices[piece] = maskIndex;
    }

    public void UnregisterPiece(ChessPiece piece)
    {
        realTypes.Remove(piece);
        maskIndices.Remove(piece);
    }

    public void UndoPendingMove()
    {
        if (pendingMovePiece == null) return;

        var board = ChessBoard.Instance.board;

        // Move piece back to original position
        board[pendingTo.x, pendingTo.y] = null;
        board[pendingFrom.x, pendingFrom.y] = pendingMovePiece;
        pendingMovePiece.x = pendingFrom.x;
        pendingMovePiece.y = pendingFrom.y;
        pendingMovePiece.transform.position = ChessBoard.Instance.VisualPos(pendingFrom.x, pendingFrom.y);
        pendingMovePiece.hasMoved = pendingHadMoved;

        // Restore captured piece if any
        if (capturedByPending != null)
        {
            capturedByPending.gameObject.SetActive(true);
            board[pendingTo.x, pendingTo.y] = capturedByPending;
        }
    }

    public void OnMoveAccepted()
    {
        // Destroy captured piece permanently
        if (capturedByPending != null)
        {
            if (isSinglePlayer)
                BluffyAI.Instance.UpdateBeliefsOnLoss(capturedByPending);
            UnregisterPiece(capturedByPending);
            Object.Destroy(capturedByPending.gameObject);
            capturedByPending = null;
        }

        // Update beliefs on accepted move
        if (isSinglePlayer && pendingMovePiece != null)
            BluffyAI.Instance.UpdateBeliefsOnMove(pendingMovePiece, pendingFrom, pendingTo);

        // Check if king was captured
        if (IsKingCaptured(out PieceColor winner))
        {
            currentPhase = BluffyPhase.GameOver;
            GameLogger.Instance?.EndGame($"{winner.ToString().ToLower()}_wins");
            UIManager.Instance.HideBluffPanel();
            UIManager.Instance.ShowBluffyGameOver(winner);
            ClearPendingMove();
            return;
        }

        UIManager.Instance.HideBluffPanel();
        currentPhase = BluffyPhase.Playing;

        // Turn passes to opponent
        PieceColor nextTurn = pendingMoveColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
        ClearPendingMove();
        GameManager.Instance.currentTurn = nextTurn;

        if (isOnline)
        {
            RefreshPerspective(NetworkManager.Instance.MyColor);
            UIManager.Instance.UpdateBluffyTurnText(nextTurn);
        }
        else if (isSinglePlayer)
        {
            RefreshPerspective(PieceColor.White);
            if (nextTurn == PieceColor.Black)
            {
                TriggerAITurn();
            }
            else
            {
                UIManager.Instance.UpdateBluffyTurnText(PieceColor.White);
            }
        }
        else
        {
            UIManager.Instance.UpdateBluffyTurnText(nextTurn);
        }
    }

    public void EndBluffyTurn(bool isBigPiece, PieceColor moverColor)
    {
        lastMoveWasBigPiece = isBigPiece;

        // Pawn captures: immediate game over (can't bluff pawns)
        // Big piece captures: opponent gets bluff chance first
        if (!isBigPiece && IsKingCaptured(out PieceColor winner))
        {
            currentPhase = BluffyPhase.GameOver;
            GameLogger.Instance?.EndGame($"{winner.ToString().ToLower()}_wins");
            UIManager.Instance.ShowBluffyGameOver(winner);
            return;
        }

        if (isOnline)
        {
            PieceColor opponent = moverColor == PieceColor.White ? PieceColor.Black : PieceColor.White;

            if (isBigPiece)
            {
                // The opponent needs to decide: bluff or accept
                // If it was remote move → show bluff panel to us
                // If it was our move → wait for remote bluff/accept (poll handles it)
                if (moverColor != NetworkManager.Instance.MyColor)
                {
                    currentPhase = BluffyPhase.WaitingBluff;
                    RefreshPerspective(NetworkManager.Instance.MyColor);
                    UIManager.Instance.ShowBluffPanel();
                }
                else
                {
                    currentPhase = BluffyPhase.WaitingBluff;
                    UIManager.Instance.UpdateBluffyTurnText(opponent);
                }
            }
            else
            {
                currentPhase = BluffyPhase.Playing;
                GameManager.Instance.currentTurn = opponent;
                RefreshPerspective(NetworkManager.Instance.MyColor);
                UIManager.Instance.UpdateBluffyTurnText(opponent);
            }
            return;
        }

        if (isSinglePlayer)
        {
            PieceColor opponent = moverColor == PieceColor.White ? PieceColor.Black : PieceColor.White;

            if (isBigPiece)
            {
                if (moverColor == PieceColor.White)
                {
                    currentPhase = BluffyPhase.WaitingBluff;
                    BluffyAI.Instance.StartBluffDecision();
                }
                else
                {
                    currentPhase = BluffyPhase.WaitingBluff;
                    RefreshPerspective(PieceColor.White);
                    UIManager.Instance.ShowBluffPanel();
                }
            }
            else
            {
                currentPhase = BluffyPhase.Playing;
                GameManager.Instance.currentTurn = opponent;
                RefreshPerspective(PieceColor.White);

                if (opponent == PieceColor.Black)
                {
                    TriggerAITurn();
                }
                else
                {
                    UIManager.Instance.UpdateBluffyTurnText(PieceColor.White);
                }
            }
        }
        else
        {
            // Local mode: pass device to opponent
            PieceColor opponent = moverColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
            passDeviceTarget = opponent;
            currentPhase = BluffyPhase.PassDevice;
            ShowPassDeviceDelayed(opponent);
        }
    }

    private void TriggerAITurn()
    {
        UIManager.Instance.UpdateBluffyTurnText(PieceColor.Black);
        BluffyAI.Instance.PlayTurn();
    }

    private void FinishRearrangeOnline()
    {
        PieceColor opponent = pendingMoveColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
        ClearPendingMove();
        lastMoveWasBigPiece = false;
        currentPhase = BluffyPhase.Playing;
        GameManager.Instance.currentTurn = opponent;
        RefreshPerspective(NetworkManager.Instance.MyColor);
        UIManager.Instance.UpdateBluffyTurnText(opponent);
    }

    private void ShowPassDeviceDelayed(PieceColor target)
    {
        StartCoroutine(PassDeviceDelayCoroutine(target));
    }

    private IEnumerator PassDeviceDelayCoroutine(PieceColor target)
    {
        yield return new WaitForSeconds(settings.passDeviceDelay);
        UIManager.Instance.ShowPassDevicePanel(target);
    }

    // ─── Online: Setup Serialization ───

    private string SerializeBackRank(PieceColor color)
    {
        int backY = color == PieceColor.White ? 0 : 7;
        var board  = ChessBoard.Instance.board;
        string[] types = new string[8];
        for (int x = 0; x < 8; x++)
        {
            ChessPiece p = board[x, backY];
            if (p != null && realTypes.ContainsKey(p))
                types[x] = realTypes[p].ToString();
            else if (p != null)
                types[x] = p.type.ToString();
            else
                types[x] = "None";
        }
        return string.Join(",", types);
    }

    private string SerializeMyBackRank()
    {
        PieceColor myColor = NetworkManager.Instance.MyColor;
        int backY = myColor == PieceColor.White ? 0 : 7;
        var board = ChessBoard.Instance.board;

        // Serialize real types of back rank pieces (8 entries)
        string[] types = new string[8];
        for (int x = 0; x < 8; x++)
        {
            ChessPiece p = board[x, backY];
            if (p != null && realTypes.ContainsKey(p))
                types[x] = realTypes[p].ToString();
            else if (p != null)
                types[x] = p.type.ToString();
            else
                types[x] = "None";
        }
        return string.Join(",", types);
    }

    public void OnBothSetupsReady(string opponentPositionsJson, bool isHost)
    {
        UIManager.Instance.HideInfoPanel();

        // Apply opponent's back rank setup
        PieceColor opponentColor = isHost ? PieceColor.Black : PieceColor.White;
        int opponentBackY = opponentColor == PieceColor.White ? 0 : 7;
        var board = ChessBoard.Instance.board;

        string[] types = opponentPositionsJson.Split(',');
        for (int x = 0; x < 8 && x < types.Length; x++)
        {
            ChessPiece p = board[x, opponentBackY];
            if (p == null) continue;
            if (p.type == PieceType.Pawn) continue;

            if (System.Enum.TryParse(types[x], out PieceType pt))
            {
                realTypes[p] = pt;
                p.UpdateType(pt);
            }
        }

        GameLogger.Instance?.LogBluffySetup(opponentColor, opponentPositionsJson);
        RandomizeMaskIndices();
        setupComplete = true;
        currentPhase = BluffyPhase.Playing;
        GameManager.Instance.currentTurn = PieceColor.White;
        RefreshPerspective(NetworkManager.Instance.MyColor);
        UIManager.Instance.UpdateBluffyTurnText(PieceColor.White);
    }

    // ─── Online: Remote Action Handlers ───

    public void ApplyRemoteSacrifice(int x, int y)
    {
        var board = ChessBoard.Instance.board;
        ChessPiece piece = board[x, y];
        if (piece == null) return;

        GameLogger.Instance?.LogSacrifice(x, y);
        UnregisterPiece(piece);
        board[x, y] = null;
        Object.Destroy(piece.gameObject);

        // Now rearrange phase - if it's our turn to rearrange
        if (rearrangeColor == NetworkManager.Instance.MyColor)
        {
            currentPhase = BluffyPhase.Rearrange;
            selectedSwapPiece = null;
            RefreshPerspective(NetworkManager.Instance.MyColor);
            if (movedPiece != null)
                ChessBoard.Instance.HighlightSquare(movedPiece.x, movedPiece.y, ChessBoard.Instance.selectedColor);
            UIManager.Instance.ShowRearrangePanel(rearrangeColor);
        }
        else
        {
            // Wait for remote rearrange
            currentPhase = BluffyPhase.Rearrange;
            UIManager.Instance.UpdateBluffyTurnText(rearrangeColor);
        }
    }

    public void ApplyRemoteRearrangeSwap(int x1, int y1, int x2, int y2)
    {
        var board = ChessBoard.Instance.board;
        ChessPiece p1 = board[x1, y1];
        ChessPiece p2 = board[x2, y2];
        if (p1 != null && p2 != null)
        {
            GameLogger.Instance?.LogRearrangeSwap(x1, y1, x2, y2);
            SwapPiecePositionsKeepMasks(p1, p2);
        }

        FinishRearrangeOnline();
    }

    public void ApplyRemoteRearrangeSkip()
    {
        FinishRearrangeOnline();
    }

    private void ClearPendingMove()
    {
        pendingMovePiece = null;
        capturedByPending = null;
        movedPiece = null;
    }
}
