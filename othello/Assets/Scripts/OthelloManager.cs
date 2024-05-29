using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OthelloManager : MonoBehaviourPunCallbacks
{
    #region singleton
    public static OthelloManager instance { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region ���� ����
    public Transform gameBoard;

    #region ������ ��ư
    private static int BOARD_SIZE = 8;
    private Button[,] boards = new Button[BOARD_SIZE, BOARD_SIZE];
    #endregion
    #endregion

    #region ����
    public List<GameObject> userList = new();
    #endregion

    private void Start()
    {
        PhotonNetwork.IsMessageQueueRunning = true;

        // ������ �ʱ�ȭ
        for (int i = 0; i < gameBoard.childCount; i++)
        {
            boards[i / BOARD_SIZE, i % BOARD_SIZE] = gameBoard.GetChild(i).gameObject.GetComponent<Button>();
        }
    }

    public void SetUser(int idx, string name, Sprite sprite)
    {
        userList[idx].transform.GetChild(0).GetComponent<Image>().sprite = sprite;
        userList[idx].transform.GetChild(1).GetComponent<TMP_Text>().text = name;
        userList[idx].GetComponent<Image>().fillCenter = false; // �ڱ� ���� �� true
    }
}
