using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WebSocketSharp;

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

    #region 게임 진행
    public Transform gameBoard;
    private int turn = 0;
    private static readonly int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
    private static readonly int[] dy = { 1, 1, 0, -1, -1, -1, 0, 1 };
    private List<int[]> currentInteractablePositionList = new();

    #region 포지션 버튼
    private static int BOARD_SIZE = 8;
    private Button[,] boards = new Button[BOARD_SIZE, BOARD_SIZE];
    private int[,] states = new int[BOARD_SIZE, BOARD_SIZE];
    #endregion
    #endregion

    #region 유저
    public List<GameObject> userList = new();
    private Dictionary<int, GameObject> playerDict = new(); // 실제 게임을 플레이하는 유저들
    private Dictionary<int, Sprite> playerCharacter = new(); // 플레이어 캐릭터 스프라이트

    private int otherActorNumber;
    #endregion

    #region 게임 종료
    public GameObject resultPanel;
    public TMP_Text title;
    public TMP_Text score;
    #endregion

    #region 채팅
    public TMP_InputField gameInputField;
    public Transform gameChatScrollContent;
    public GameObject gameChatTemplate;
    #endregion

    private void Start()
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        gameChatTemplate.SetActive(false);

        // 포지션 초기화
        for (int i = 0; i < gameBoard.childCount; i++)
        {
            boards[i / BOARD_SIZE, i % BOARD_SIZE] = gameBoard.GetChild(i).gameObject.GetComponent<Button>();
            states[i / BOARD_SIZE, i % BOARD_SIZE] = 0;
        }

        resultPanel.SetActive(false);
    }

    private void Update()
    {
        // 게임 채팅 입력
        if (!gameInputField.isFocused && (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return)))
        {
            if (!gameInputField.text.IsNullOrEmpty())
            {
                photonView.RPC("AddGameChatHistory", RpcTarget.All, PhotonNetwork.LocalPlayer.NickName + ": " + gameInputField.text);
                gameInputField.text = "";
            }
        }
    }

    #region 게임 진행
    public void GameStart()
    {
        // 게임 채팅 초기화
        for (int i = 1; i < gameChatScrollContent.childCount; i++)
            Destroy(gameChatScrollContent.GetChild(i).gameObject);
        gameInputField.text = "";

        // 버튼 세팅
        for (int i = 0; i < BOARD_SIZE; i++)
            for (int j = 0; j < BOARD_SIZE; j++)
            {
                states[i, j] = 0;
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

        // 마스터부터 시작
        photonView.RPC("NextTurn", RpcTarget.All, PhotonNetwork.MasterClient.ActorNumber);
        SetEnablePosition(false);
    }

    // 돌을 놓을 수 있는가?
    private bool IsEnableToSet(int x, int y, int actorNumber)
    {
        if (states[x, y] != 0) return false;
        for (int i = 0; i < dx.Length; i++)
        {
            int xx = x + dx[i];
            int yy = y + dy[i];
            // 자신이 놓을 돌과 자신의 돌 사이에 상대편의 돌이 있어야 돌을 놓을 수 있음
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

    // prevSkiped: 앞턴에서 놓을 말이 없어서 스킵됐는가?
    [PunRPC]
    private void SetEnablePosition(bool prevSkiped)
    {
        currentInteractablePositionList.Clear();
        if (turn != PhotonNetwork.LocalPlayer.ActorNumber) return;

        for (int i = 0; i < BOARD_SIZE; i++)
        {
            for (int j = 0; j < BOARD_SIZE; j++)
            {
                // 돌을 놓을 수 있는 위치의 버튼 활성화
                if (IsEnableToSet(i, j, PhotonNetwork.LocalPlayer.ActorNumber))
                {
                    boards[i, j].interactable = true;
                    boards[i, j].transform.GetChild(0).GetComponent<Image>().color = Color.white;
                    currentInteractablePositionList.Add(new int[2] { i, j });
                }
            }
        }

        // 활성화 할 수 있는 버튼이 없으면 턴 종료
        if (currentInteractablePositionList.Count == 0)
        {
            if (prevSkiped) photonView.RPC("GameFinished", RpcTarget.All, -1);
            else photonView.RPC("NextTurn", RpcTarget.All, otherActorNumber);
        }
    }

    [PunRPC]
    private void GameFinished(int leftPerson)
    {
        int mine = 0, other = 0;
        if (leftPerson <= 0) // 아무도 나가지 않고 정상 종료
        {
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    if (states[i, j] == PhotonNetwork.LocalPlayer.ActorNumber) mine++;
                    else if (states[i, j] == otherActorNumber) other++;
                }
            }
            score.text = PhotonNetwork.LocalPlayer.NickName + ": " + mine + "\r\n"
            + PhotonNetwork.CurrentRoom.Players[otherActorNumber].NickName + ": " + other;
        }
        else if (leftPerson != otherActorNumber) // 내가 나간 경우
        {
            other = 100;
            score.text = "게임을 포기했습니다..";
        }
        else // 상대방이 나간 경우
        {
            mine = 100;
            score.text = "상대방이 게임을 포기했습니다!";
        }

        title.text = mine > other ? "승리!" : (mine == other ? "비겼다.." : "졌다..");
        resultPanel.SetActive(true);

        playerDict.Clear();
        playerCharacter.Clear();

        PhotonNetwork.CurrentRoom.SetMasterClient(mine > other ? PhotonNetwork.LocalPlayer : PhotonNetwork.CurrentRoom.Players[otherActorNumber]);
    }

    public void OnClickLeave()
    {
        photonView.RPC("GameFinished", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    public void OnClickPosition()
    {
        string currentSelectedName = EventSystem.current.currentSelectedGameObject.name;
        // 현재 활성화 되어 있는 모든 버튼 다시 비활성화
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

        // 상대방 말 먹고 턴 종료
        photonView.RPC("SetTurnResult", RpcTarget.All, clicked[0], clicked[1], PhotonNetwork.LocalPlayer.ActorNumber);
        photonView.RPC("NextTurn", RpcTarget.All, otherActorNumber);
    }

    // x, y에서 시작해서 뒤집을 수 있는 모든 말을 뒤집음
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
            // 자신이 놓을 돌과 자신의 돌 사이에 상대편의 돌이 있어야 돌을 놓을 수 있음
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
        SetEnablePosition(currentInteractablePositionList.Count == 0);
    }
    #endregion

    public void SetUser(int idx, int actorNumber, string name, Sprite sprite)
    {
        userList[idx].transform.GetChild(0).GetComponent<Image>().sprite = sprite;
        userList[idx].transform.GetChild(1).GetComponent<TMP_Text>().text = name;
        userList[idx].GetComponent<Image>().fillCenter = false; // 자기 턴일 때 true

        playerDict.Add(actorNumber, userList[idx]);
        playerCharacter.Add(actorNumber, sprite);
    }

    #region 채팅
    [PunRPC]
    private void AddGameChatHistory(string msg)
    {
        GameObject obj = Instantiate(gameChatTemplate, gameChatScrollContent);
        obj.GetComponent<TMP_Text>().text = msg;
        obj.SetActive(true);
    }

    #endregion
}
