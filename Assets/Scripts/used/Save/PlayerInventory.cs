//using System.Collections.Generic;
//using UnityEngine;

//public class PlayerInventory : MonoBehaviour
//{
//    private readonly HashSet<string> items = new HashSet<string>();

//    public void AddById(string itemId)
//    {
//        if (!string.IsNullOrEmpty(itemId))
//            items.Add(itemId);
//    }

//    public void ClearAll() => items.Clear();

//    public string[] GetAllItemIds()
//    {
//        var arr = new string[items.Count];
//        items.CopyTo(arr);
//        return arr;
//    }

//    // остальное как было...
//}
