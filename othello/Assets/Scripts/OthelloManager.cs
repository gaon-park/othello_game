using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private int turn = 0;
    private static readonly int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
    private static readonly int[] dy = { 1, 1, 0, -1, -1, -1, 0, 1 };
    private List<int[]> currentInteractablePositionList = new();

    #region ������ ��ư
    private static int BOARD_SIZE = 8;
    private Button[,] boards = new Button[BOARD_SIZE, BOARD_SIZE];
    private int[,] states = new int[BOARD_SIZE, BOARD_SIZE];
    #endregion
    #endregion

    #region ����
    public List<GameObject> userList = new();
    private Dictionary<int, GameObject> playerDict = new(); // ���� ������ �÷����ϴ� ������
    private Dictionary<int, Sprite> playerCharacter = new(); // �÷��̾� ĳ���� ��������Ʈ

    private int otherActorNumber;
    #endregion

    private void Start()
    {
        PhotonNetwork.IsMessageQueueRunning = true;

        // ������ �ʱ�ȭ
        for (int i = 0; i < gameBoard.childCount; i++)
        {
            boards[i / BOARD_SIZE, i % BOARD_SIZE] = gameBoard.GetChild(i).gameObject.GetComponent<Button>();
            states[i / BOARD_SIZE, i % BOARD_SIZE] = 0;
        }
    }

    #region ���� ����
    public void GameStart()
    {
        // ��ư ����
        for (int i = 0; i < BOARD_SIZE; i++)
            for (int j = 0; j < BOARD_SIZE; j++)
            {
                boards[i, j].interactable = false;
                boards[i, j].transform.GetChild(0).GetComponent<Image>().color = Color.gray;
            }

        int clientActorNumber = playerDict.Keys
            .Where(o => o != PhotonNetwork.MasterClient.ActorNumber)
            .First();

        for (int i = 3; i <= 4; i++)
        {
            for (int j = 3; j <= 4; j++)
            {
                int actorNumber = (i == j) ? PhotonNetwork.MasterClient.ActorNumber : clientActorNumber;
                boards[i, j].transform.GetChild(0).GetComponent<Image>().sprite = playerCharacter[actorNumber];
                boards[i, j].transform.GetChild(0).GetComponent<Image>().color = Color.white;
                states[i, j] = actorNumber;
            }
        }

        otherActorNumber = playerDict.Keys
            .Where(o => o != PhotonNetwork.LocalPlayer.ActorNumber)
            .First();

        // �����ͺ��� ����
        photonView.RPC("NextTurn", RpcTarget.All, PhotonNetwork.MasterClient.ActorNumber);
        SetEnablePosition();
    }

    // ���� ���� �� �ִ°�?
    private bool IsEnableToSet(int x, int y, int actorNumber)
    {
        if (states[x, y] != 0) return false;
        for (int i = 0; i < dx.Length; i++)
        {
            int xx = x + dx[i];
            int yy = y + dy[i];
            // �ڽ��� ���� ���� �ڽ��� �� ���̿� ������� ���� �־�� ���� ���� �� ����
            if (xx < 0 || yy < 0 || xx >= BOARD_SIZE || yy >= BOARD_SIZE || states[xx, yy] == 0 || states[xx, yy] == actorNumber) continue;
            if (IsEnableToSetThisWay(i, xx + dx[i], yy + dy[i], actorNumber)) return true;
        }
        return false;
    }

    private bool IsEnableToSetThisWay(int way, int x, int y, int actorNumber)
    {
        if (x < 0 || y < 0 || x >= BOARD_SIZE || y >= BOARD_SIZE || states[x, y] == 0) return false;
        if (states[x, y] == actorNumber) return true;
        return IsEnableToSetThisWay(way, x + dx[way], y + dy[way], actorNumber);
    }

    [PunRPC]
    private void SetEnablePosition()
    {
        currentInteractablePositionList.Clear();
        if (turn != PhotonNetwork.LocalPlayer.ActorNumber) return;

        for (int i = 0; i < BOARD_SIZE; i++)
        {
            for (int j = 0; j < BOARD_SIZE; j++)
            {
                // ���� ���� �� �ִ� ��ġ�� ��ư Ȱ��ȭ
                if (IsEnableToSet(i, j, PhotonNetwork.LocalPlayer.ActorNumber))
                {
                    boards[i, j].interactable = true;
                    boards[i, j].transform.GetChild(0).GetComponent<Image>().color = Color.white;
                    currentInteractablePositionList.Add(new int[2] { i, j });
                }
            }
        }

        // Ȱ��ȭ �� �� �ִ� ��ư�� ������ �� ����
        if (currentInteractablePositionList.Count == 0)
            photonView.RPC("NextTurn", RpcTarget.All, otherActorNumber);
    }

    public void OnClickPosition()
    {
        string currentSelectedName = EventSystem.current.currentSelectedGameObject.name;
        // ���� Ȱ��ȭ �Ǿ� �ִ� ��� ��ư �ٽ� ��Ȱ��ȭ
        int[] clicked = new int[2];
        for (int i = 0; i < currentInteractablePositionList.Count; i++)
        {
            int[] pos = currentInteractablePositionList[i];
            boards[pos[0], pos[1]].interactable = false;
            boards[pos[0], pos[1]].transform.GetChild(0).GetComponent<Image>().color = Color.gray;

            if (boards[pos[0], pos[1]].gameObject.name == currentSelectedName)
            {
                clicked[0] = pos[0];
                clicked[1] = pos[1];
            }
        }

        // ���� �� �԰� �� ����
        photonView.RPC("SetTurnResult", RpcTarget.All, clicked[0], clicked[1], PhotonNetwork.LocalPlayer.ActorNumber);
        photonView.RPC("NextTurn", RpcTarget.All, otherActorNumber);
    }

    // x, y���� �����ؼ� ������ �� �ִ� ��� ���� ������
    [PunRPC]
    private void SetTurnResult(int x, int y, int actorNumber)
    {
        boards[x, y].transform.GetChild(0).GetComponent<Image>().sprite = playerCharacter[actorNumber];
        boards[x, y].transform.GetChild(0).GetComponent<Image>().color = Color.white;
        states[x, y] = actorNumber;
        for (int i = 0; i < dx.Length; i++)
        {
            int xx = x + dx[i];
            int yy = y + dy[i];
            // �ڽ��� ���� ���� �ڽ��� �� ���̿� ������� ���� �־�� ���� ���� �� ����
            if (xx < 0 || yy < 0 || xx >= BOARD_SIZE || yy >= BOARD_SIZE || states[xx, yy] == 0 || states[xx, yy] == actorNumber) continue;
            if (IsEnableToSetThisWay(i, xx + dx[i], yy + dy[i], actorNumber)) SetTurnResultToThisWay(i, xx, yy, actorNumber);
        }
    }

    private void SetTurnResultToThisWay(int way, int x, int y, int actorNumber)
    {
        if (x < 0 || y < 0 || x >= BOARD_SIZE || y >= BOARD_SIZE || states[x, y] == 0 || states[x, y] == actorNumber) return;

        boards[x, y].transform.GetChild(0).GetComponent<Image>().sprite = playerCharacter[actorNumber];
        boards[x, y].transform.GetChild(0).GetComponent<Image>().color = Color.white;
        states[x, y] = actorNumber;
        SetTurnResultToThisWay(way, x + dx[way], y + dy[way], actorNumber);
    }

    [PunRPC]
    private void NextTurn(int turn)
    {
        if (this.turn != 0) playerDict[this.turn].GetComponent<Image>().fillCenter = false;
        this.turn = turn;
        playerDict[this.turn].GetComponent<Image>().fillCenter = true;
        SetEnablePosition();
    }
    #endregion

    public void SetUser(int idx, int actorNumber, string name, Sprite sprite)
    {
        userList[idx].transform.GetChild(0).GetComponent<Image>().sprite = sprite;
        userList[idx].transform.GetChild(1).GetComponent<TMP_Text>().text = name;
        userList[idx].GetComponent<Image>().fillCenter = false; // �ڱ� ���� �� true

        playerDict.Add(actorNumber, userList[idx]);
        playerCharacter.Add(actorNumber, sprite);
    }
}
