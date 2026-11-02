using UnityEngine;

public class ZonaTrigger : MonoBehaviour
{
    public enum ZoneType { Win, Lose }

    [SerializeField] private ZoneType zoneType;
    [SerializeField] private EndZone endZone;   // arrastra el único EndZone

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Pelota")) return;
        Destroy(other.gameObject);

        endZone.MostrarFinal(zoneType == ZoneType.Win);
    }
}