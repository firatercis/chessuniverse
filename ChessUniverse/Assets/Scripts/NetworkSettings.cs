using UnityEngine;

[CreateAssetMenu(fileName = "NetworkSettings", menuName = "Chess/NetworkSettings")]
public class NetworkSettings : ScriptableObject
{
    public string firebaseProjectUrl = "https://your-project.firebaseio.com";
    public float pollIntervalSeconds = 0.5f;
    public float connectionTimeoutSeconds = 5f;
}
