using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class buttonInfo
{
    public buttonInfo(float n_price, int n_rank, float n_value, string n_name)
    {
        price = n_price;
        rank = n_rank;
        value = n_value;
        name = n_name;
    }

    public float price { get; set; }
    public int rank { get; set; }
    public float value { get; set; }
    public string name { get; }
    public void IncreaseRank()
    {
        rank++;
        price *= 1.2f;
        value *= 1.15f;
    }
}

public class Upgrades : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    private float pumpkinCount = 0;
    public float totalPumpkinCount { get; set; }

    private float farmerValue = 0;

    private float pumpkinValue = 1;

    [SerializeField] private TileManager tileMgr;
    [SerializeField] private List<Button> rowLocks;
    private List<float> rowCosts = new List<float> { 25, 50, 100, 200 };

    [SerializeField] private List<Button> buttons = new List<Button>();
    private List<buttonInfo> buttonInfoList = new List<buttonInfo>();

    [SerializeField]
    // A prefab for the rototiller.
    private Automater rototillerPrefab;
    // A prefab for the rototiller's gravestone
    [SerializeField] private GameObject gravestone;
    [SerializeField]
    // A prefab for the farmer.
    private Automater farmerPrefab;

    private float timer;

    private void Start()
    {
        buttonInfo fertilizer = new buttonInfo(5, 0, 0, "fertilizer");
        buttonInfo biggerPump = new buttonInfo(10, 0, 0, "biggerPump");
        buttonInfo farmers = new buttonInfo(20, 0, 0.01f, "farmers");
        buttonInfo rototillers = new buttonInfo(60, 0, 0.1f, "rototillers");
        buttonInfo factory = new buttonInfo(120, 0, 0.5f, "factory");
        buttonInfo labs = new buttonInfo(200, 0, 1f, "labs");

        buttonInfoList.Add(fertilizer);
        buttonInfoList.Add(biggerPump);
        buttonInfoList.Add(farmers);
        buttonInfoList.Add(rototillers);
        buttonInfoList.Add(factory);
        buttonInfoList.Add(labs);   
    }

    private void Update()
    {
        timer += Time.deltaTime;
        scoreText.text = "Pumpkins: " + Math.Round(pumpkinCount);

        for(int i = 0; i < buttons.Count; i++)
        {
            buttons[i].GetComponentInChildren<TextMeshProUGUI>().text = "Price: " + Math.Round(buttonInfoList[i].price, 1) + "\n\n\nRank: " + buttonInfoList[i].rank;
            if ((buttonInfoList[i].name == "rototillers" || buttonInfoList[i].name == "farmers") && buttonInfoList[i].rank == 10)
            {
                buttons[i].GetComponentInChildren<TextMeshProUGUI>().text = "Price: --\n\n\nRank: MAX";
            }
        }



        for (int i = 0; i < buttons.Count; i++)
        {
            if ((buttonInfoList[i].name == "rototillers" && buttonInfoList[i].rank >= 10) ||
                ((buttonInfoList[i].name == "farmers" && buttonInfoList[i].rank >= 10)))
            {
                buttons[i].interactable = false;
                continue;
            }
            if (pumpkinCount < buttonInfoList[i].price)
            {
                buttons[i].interactable = false;
            }
            else { buttons[i].interactable = true; }
        }
        for (int i = 0; i < rowLocks.Count; i++)
        {
            if (pumpkinCount < rowCosts[i] && rowLocks[i].interactable)
                rowLocks[i].interactable = false;
            else if (pumpkinCount >= rowCosts[i] && !rowLocks[i].interactable)
                rowLocks[i].interactable = true;

        }

        if (timer > 0.25)
        {
            pumpkinCount += farmerValue;
            timer = 0;
        }
    }

    public void upgrade(string buttonName)
    {
        for(int i = 0; i < buttonInfoList.Count; i++)
        {
            if (buttonInfoList[i].name == buttonName)
            {
                pumpkinCount -= buttonInfoList[i].price;

                if (buttonInfoList[i].name == "fertilizer")
                {
                    tileMgr.DecreaseGrowTime();
                }
                else if (buttonInfoList[i].name == "biggerPump")
                {
                    pumpkinValue += 0.5f;
                }
                else
                {
                    if (buttonInfoList[i].name == "rototillers")
                    {
                        Instantiate(rototillerPrefab, tileMgr.transform).AssignManagers(tileMgr, this, gravestone, automatorType.Harvester);
                    }
                    else if (buttonInfoList[i].name == "farmers")
                    {
                        Instantiate(farmerPrefab, tileMgr.transform).AssignManagers(tileMgr, this, gravestone, automatorType.Planter);
                    }
                    farmerValue += buttonInfoList[i].value;
                }
                buttonInfoList[i].IncreaseRank();
            }
        }
    }

    public void IncreasePumpkin(int value)
    {
        pumpkinCount += pumpkinValue * value;
        totalPumpkinCount += pumpkinValue * value;
    }

    public void RemoveRowLock(Button rowLock)
    {
        int lockIdx = rowLocks.IndexOf(rowLock);
        pumpkinCount -= rowCosts[lockIdx];
        rowCosts.RemoveAt(lockIdx);
        Destroy(rowLock.gameObject);
        rowLocks.RemoveAt(lockIdx);
    }

    public void UpgradeViaKeypress(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            string upgradeName;
            int buttonId;
            switch (ctx.control.displayName)
            {
                case "2":
                    upgradeName = "biggerPump";
                    buttonId = 1;
                    break;
                case "3":
                    upgradeName = "farmers";
                    buttonId = 2;
                    break;
                case "4":
                    upgradeName = "rototillers";
                    buttonId = 3;
                    break;
                case "5":
                    upgradeName = "factory";
                    buttonId = 4;
                    break;
                case "6":
                    upgradeName = "labs";
                    buttonId = 5;
                    break;
                default:
                    upgradeName = "fertilizer";
                    buttonId = 0;
                    break;
            }

            if (pumpkinCount >= buttonInfoList[buttonId].price)
            {
                if ((buttonId == 2 || buttonId == 3) && buttonInfoList[buttonId].rank >= 10) 
                    return; // Make sure you can't buy more helpers than possible
                upgrade(upgradeName);
            }
        }
    }
}
