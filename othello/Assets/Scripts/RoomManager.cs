using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager instance { get; private set; }

    #region 패널
    public GameObject lobbyPanel;
    public GameObject roomPanel;
    public GameObject othelloPanel;
    #endregion

    #region 유저 리스트
    public Transform userList;
    public GameObject userTemplate;

    // 캐릭터 리소스
    private static readonly string USER_IMGS = "character";
    private Dictionary<string, Sprite> sprites = new();
    #endregion

    #region 채팅 기록
    public Transform gameChatScrollContent;
    public GameObject gameChatTemplate;

    public Transform roomChatScrollContent;
    public GameObject roomChatTemplate;
    #endregion

    #region 채팅 입력
    public TMP_InputField roomInputField;
    public TMP_InputField gameInputField;
    #endregion

    #region UI 입력
    public Button readyButton;
    public Button leaveButton;
    #endregion

    #region 게임 진행
    private bool isPlaying = false;
    #endregion

    #region
    private static readonly string READY_KEY = "ready";
    #endregion

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

    void Start()
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        othelloPanel.SetActive(false);
        userTemplate.SetActive(false);
        roomChatTemplate.SetActive(false);
        gameChatTemplate.SetActive(false);

        readyButton.onClick.AddListener(OnClickReady);
        leaveButton.onClick.AddListener(OnClickLeft);

        // 캐릭터 리소스 설정
        foreach (Sprite s in Resources.LoadAll<Sprite>(USER_IMGS))
            sprites.Add(s.name, s);
    }

    private void Update()
    {
        // 룸 채팅 입력
        if (!roomInputField.isFocused && (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return)))
        {
            if (!roomInputField.text.IsNullOrEmpty())
            {
                photonView.RPC("AddChatHistory", RpcTarget.All, PhotonNetwork.LocalPlayer.NickName + ": " + roomInputField.text);
                roomInputField.text = "";
            }
        }

        // 게임 채팅 입력
        else if (!gameInputField.isFocused && (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return)))
        {
            if (!gameInputField.text.IsNullOrEmpty())
            {
                photonView.RPC("AddChatHistory", RpcTarget.All, PhotonNetwork.LocalPlayer.NickName + ": " + gameInputField.text);
                gameInputField.text = "";
            }
        }
    }

    #region 준비
    public void OnClickReady()
    {
        ExitGames.Client.Photon.Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
        int[] readyArray = properties[READY_KEY] as int[];
        List<int> readyList = new(readyArray);

        // 마스터 클라이언트: 시작
        if (PhotonNetwork.IsMasterClient)
        {
            // 마스터 이외에 모두 준비 완료라면 게임 플레이
            if (PhotonNetwork.CurrentRoom.PlayerCount > 1 && readyList.Count + 1 == PhotonNetwork.CurrentRoom.PlayerCount)
            {
                // 마스터 레디 정보 입력
                readyList.Add(PhotonNetwork.LocalPlayer.ActorNumber);
                properties["ready"] = readyList.ToArray();
                PhotonNetwork.CurrentRoom.SetCustomProperties(properties);

                // 캐릭터 랜덤 설정
                string[] keys = sprites.Keys.ToArray();
                int randomIndex1 = Random.Range(0, keys.Length);
                int randomIndex2;
                do
                {
                    randomIndex2 = Random.Range(0, keys.Length);
                } while (randomIndex2 == randomIndex1);

                // 게임 시작
                photonView.RPC("GameStart", RpcTarget.All, keys[randomIndex1], keys[randomIndex2]);
            }
        }
        // 그외: 준비
        else
        {
            if (readyList.Contains(PhotonNetwork.LocalPlayer.ActorNumber)) readyList.Remove(PhotonNetwork.LocalPlayer.ActorNumber);
            else readyList.Add(PhotonNetwork.LocalPlayer.ActorNumber);

            properties[READY_KEY] = readyList.ToArray();
            PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);

        // 변경된 프로퍼티
        foreach (DictionaryEntry entry in propertiesThatChanged)
        {
            Debug.Log($"Room property {entry.Key} changed to {entry.Value}");

            // 준비 완료 마크 갱신
            if (entry.Key.ToString().Equals("ready"))
            {
                int[] readyArray = entry.Value as int[];
                List<int> readyList = new(readyArray);
                for (int i = 0; i < userList.childCount; i++)
                {
                    Transform obj = userList.GetChild(i);
                    if (!obj.gameObject.activeSelf) continue;

                    Debug.Log("int.Parse(obj.gameObject.name): " + int.Parse(obj.gameObject.name));
                    obj.GetChild(1).gameObject.SetActive(readyList.Contains(int.Parse(obj.gameObject.name)));
                }
            }
        }
    }

    [PunRPC]
    private void GameStart(string masterSprite, string otherSprite)
    {
        roomPanel.SetActive(false);
        othelloPanel.SetActive(true);

        isPlaying = true;

        // 유저 UI 설정
        ExitGames.Client.Photon.Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
        int[] readyArray = properties[READY_KEY] as int[];
        Dictionary<int, Player> players = PhotonNetwork.CurrentRoom.Players;

        Debug.Log("master: " + masterSprite);
        Debug.Log("other: " + otherSprite);

        int idx = 0;
        OthelloManager.instance.SetUser(idx++, PhotonNetwork.LocalPlayer.ActorNumber, PhotonNetwork.LocalPlayer.NickName, sprites[PhotonNetwork.IsMasterClient ? masterSprite : otherSprite]);
        foreach (int actorNumber in readyArray)
        {
            // 본인 캐릭터는 항상 "상단"
            if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber) continue;
            OthelloManager.instance.SetUser(idx++, actorNumber, players[actorNumber].NickName, sprites[PhotonNetwork.CurrentRoom.Players[actorNumber].IsMasterClient ? masterSprite : otherSprite]);
        }

        OthelloManager.instance.GameStart();
    }
    #endregion

    #region 방 입장/퇴장
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        lobbyPanel.SetActive(false);
        roomPanel.SetActive(true);
        othelloPanel.SetActive(false);

        // 유저 리스트 초기화
        for (int i = 0; i < userList.childCount; i++)
            if (userList.GetChild(i).gameObject.activeSelf)
                Destroy(userList.GetChild(i).gameObject);

        // 방 채팅 초기화
        for (int i = 1; i < roomChatScrollContent.childCount; i++)
            Destroy(roomChatScrollContent.GetChild(i).gameObject);

        // 게임 채팅 초기화
        for (int i = 1; i < gameChatScrollContent.childCount; i++)
            Destroy(gameChatScrollContent.GetChild(i).gameObject);

        // 입력 상자 초기화
        roomInputField.text = "";
        gameInputField.text = "";

        // 초기에 들어와 있는 사람이 있다면?
        foreach (Player p in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (!p.IsLocal) AddUser(p);
        }

        OnPlayerEnteredRoom(PhotonNetwork.LocalPlayer);

        // 룸 커스텀 프로퍼티 초기화
        if (PhotonNetwork.IsMasterClient)
        {
            ExitGames.Client.Photon.Hashtable properties = new()
            {
                { READY_KEY, new int[0] } // value: actor number
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
            isPlaying = false;
        }
    }

    private void AddUser(Player p)
    {
        GameObject obj = Instantiate(userTemplate, userList);
        obj.name = p.ActorNumber.ToString(); // 오브젝트 이름을 ActorNumber 로 설정
        obj.transform.GetChild(0).GetComponent<TMP_Text>().text = p.NickName;
        obj.transform.GetChild(1).gameObject.SetActive(false); // 준비 완료
        obj.SetActive(true);
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();

        roomPanel.SetActive(false);

        StartCoroutine(WaitAndJoinLobby());
    }

    private IEnumerator WaitAndJoinLobby()
    {
        while (!PhotonNetwork.IsConnectedAndReady)
            yield return null;

        PhotonNetwork.JoinLobby();
    }

    public void OnClickLeft()
    {
        // 방 프로퍼티에서 준비 상태 지우기
        ExitGames.Client.Photon.Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
        int[] readyArray = properties[READY_KEY] as int[];
        List<int> readyList = new(readyArray);
        if (readyList.Contains(PhotonNetwork.LocalPlayer.ActorNumber)) readyList.Remove(PhotonNetwork.LocalPlayer.ActorNumber);
        properties[READY_KEY] = readyList.ToArray();
        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);

        // 내가 마지막 클라이언트라면 방 삭제
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.RemovedFromList = true;
        }
        PhotonNetwork.LeaveRoom();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        AddUser(newPlayer);

        // 입장 알림
        AddChatHistory("'" + newPlayer.NickName + "'" + "님이 입장했습니다.");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        for (int i = 0; i < userList.childCount; i++)
        {
            GameObject obj = userList.GetChild(i).gameObject;
            if (!obj.activeSelf) continue;
            // 방금 퇴장한 사람과 닉네임이 똑같다면
            if (obj.transform.GetChild(0).GetComponent<TMP_Text>().text.Equals(otherPlayer.NickName))
            {
                Destroy(obj);
                break;
            }
        }

        // 퇴장 알림
        AddChatHistory("'" + otherPlayer.NickName + "'" + "님이 퇴장했습니다.");
    }
    #endregion

    #region 채팅
    [PunRPC]
    private void AddChatHistory(string msg)
    {
        Transform transform = isPlaying ? gameChatScrollContent : roomChatScrollContent;
        GameObject template = isPlaying ? gameChatTemplate : roomChatTemplate;
        GameObject obj = Instantiate(template, transform);
        obj.GetComponent<TMP_Text>().text = msg;
        obj.SetActive(true);
    }

    #endregion
}
