using System.Linq;
using UnityEngine;

public class SaveStationRegistry : MonoBehaviour
{
    public static SaveStation FindById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return Object.FindObjectsOfType<SaveStation>().FirstOrDefault(s => s.StationId == id);
    }
}
