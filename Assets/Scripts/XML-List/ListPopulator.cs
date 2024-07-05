using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.UI;

public class ListPopulator : MonoBehaviour
{
    public GameObject listItemPrefab; // Assign the prefab in the Inspector
    public Transform listParent; // Assign the parent transform in the Inspector
    private List<TogglePopulator> listItems = new List<TogglePopulator>();

    void Start()
    {
        RefreshList();
    }
    public void RefreshList()
    {
        foreach (var item in listItems)
        {
            Destroy(item.gameObject);
        }
        listItems.Clear();
        var xmlFiles = GetXMLFilesNames();
        if (xmlFiles == null) return;
        PopulateList(GetXMLFilesNames());
    }
    private List<string> GetXMLFilesNames()
    {
        string[] files;
        try
        {
            files = XmlReader.ListConfigFiles();
            if (!files.Any())
            {
                Debug.Log($"No config files found in data path");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error occured in reading config files, check data path folder: {e}");
            return null;
        }
        return files.ToList();
    }
    private void PopulateList(List<string> items)
    {
        foreach (string item in items)
        {
            TogglePopulator listItem = Instantiate(listItemPrefab, listParent).GetComponent<TogglePopulator>();
            listItem.Populate(item); // Use TextMeshPro if you're using TextMeshPro
            listItem.GetComponent<Toggle>().onValueChanged.AddListener((value) =>
            {
                if (value)
                    OnItemSelected(item);
            });
            listItems.Add(listItem);
        }
    }

    private void OnItemSelected(string selectedItem)
    {
        Debug.Log("Selected item: " + selectedItem);
        GameManager.Instance.UpdateXmlContent(selectedItem);
    }
}
