using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CoffeeLibrary;
using System.Linq;

public class ListViewData_Card
{
    public int index;
}


public class ListView_Card : ListView
{
    private List<ListViewData_Card> itemList = new List<ListViewData_Card>();

    [SerializeField] GameObject gameObject_listEmpty;

    public ListViewData_Card GetItemData(int index)
    {
        if (CommonUtil.inBounds(index, itemList.Count))
        {
            return itemList[index];
        }

        return null;
    }

    public void InitListView(Popup_CardBook.TAB tab)
    {
        itemList.Clear();

        List<CardData> list = SODataManager.Instance.entitySO.cardInfoList.Values.ToList();
        if (tab == Popup_CardBook.TAB.HUMAN)
        {
            list = list.Where(v => v.species == "human").ToList();
        }
        else if (tab == Popup_CardBook.TAB.ELF)
        {
            list = list.Where(v => v.species == "elf").ToList();
        }
        else if (tab == Popup_CardBook.TAB.ORC)
        {
            list = list.Where(v => v.species == "orc").ToList();
        }
        else if (tab == Popup_CardBook.TAB.UNDEAD)
        {
            list = list.Where(v => v.species == "undead").ToList();
        }
        else if (tab == Popup_CardBook.TAB.ANGEL)
        {
            list = list.Where(v => v.species == "angel").ToList();
        }       

        foreach (var item in list)
        {
            var tmp = new ListViewData_Card();
            tmp.index = item.index;
            itemList.Add(tmp);
        }

        ElementCount = itemList.Count;

        UpdateElements();
        ScrollCircuit(true);

        gameObject_listEmpty.SetActive(ElementCount == 0);
    }


    protected override void UpdateElement(ListViewItem item)
    {
        var listViewItem = item as ListViewItem_Card;
        listViewItem.RefreshInfo(itemList[item.Index]);
    }

    public static SPECIES_TYPE GetSpecies(Popup_CardBook.TAB tab) => tab switch
    {
        Popup_CardBook.TAB.HUMAN => SPECIES_TYPE.HUMAN,
        Popup_CardBook.TAB.ELF => SPECIES_TYPE.ELF,
        Popup_CardBook.TAB.ORC => SPECIES_TYPE.ORC,
        Popup_CardBook.TAB.UNDEAD => SPECIES_TYPE.UNDEAD,
        _ => SPECIES_TYPE.HUMAN
    };
}
