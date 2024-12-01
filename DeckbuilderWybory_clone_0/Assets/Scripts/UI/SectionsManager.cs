using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SectionsManager : MonoBehaviour
{
    public GameObject sectionToChange;
    public GameObject sectionFromChange;

    public void changeSection()
    {
        sectionFromChange.SetActive(false);
        sectionToChange.SetActive(true);
    }

}
