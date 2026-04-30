using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectCharacter : MonoBehaviour
{
    public GameObject[] characters;
    public int number;

    public void ChangeCharacter(int num)
    {
        for (int i = 0; i < characters.Length; i++)
        {
            characters[i].SetActive(false);
        }

        number += num;

        if (number > characters.Length-1)
        {
            number = 0;
        }

        if (number < 0)
        {
            number = characters.Length-1;
        }

        characters[number].SetActive(true);
    }
}
