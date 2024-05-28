using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WebSocketSharp;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager instance { get; private set; }

    #region 방 생성
    public TMP_InputField inputField;
    public Button createButton;
    public Transform lobbyScrollContent;
    public GameObject roomTemplate;

    private Dictionary<string, GameObject> roomDict = new();
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
    }

    private void Update()
    {
        createButton.interactable = !inputField.text.IsNullOrEmpty();
    }

    #region 방 생성
    public void OnClickJoinOrCreate()
    {
        PhotonNetwork.JoinOrCreateRoom(inputField.text, new()
        {
            MaxPlayers = NetworkManager.instance.MAX_PLAYER,
            IsOpen = true,
            IsVisible = true // 공개방
        }, null);

        inputField.text = "";
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        AddRoom(PhotonNetwork.CurrentRoom.Name, PhotonNetwork.CurrentRoom.PlayerCount);
    }

    /**
     * roomList: 변동사항 있는 방
     */
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        base.OnRoomListUpdate(roomList);

        List<string> removeList = new();
        foreach (RoomInfo roomInfo in roomList)
        {
            string key = roomInfo.Name;
            if (roomInfo.PlayerCount == 0 || roomInfo.RemovedFromList)
            {
                removeList.Add(key);
                continue;
            }

            if (roomDict.ContainsKey(key))
            {
                roomDict[key].transform.GetChild(1).gameObject.name = roomInfo.PlayerCount.ToString();
                roomDict[key].transform.GetChild(1).GetComponent<TMP_Text>().text = "(" + roomInfo.PlayerCount + "/" + NetworkManager.instance.MAX_PLAYER + ")";
            }
            else AddRoom(key, roomInfo.PlayerCount);
        }

        // 삭제된 방 지우기
        foreach (string key in removeList)
            DestoryRoom(key);
    }

    public void DestoryRoom(string name)
    {
        if (!roomDict.ContainsKey(name)) return;

        Destroy(roomDict[name]);
        roomDict.Remove(name);
    }

    public void AddRoom(string name, int count)
    {
        if (roomDict.ContainsKey(name)) return;

        GameObject newRoom = Instantiate(roomTemplate, lobbyScrollContent);

        newRoom.transform.GetChild(0).GetComponent<TMP_Text>().text = name;
        newRoom.transform.GetChild(1).GetComponent<TMP_Text>().text = "(" + count + "/" + NetworkManager.instance.MAX_PLAYER + ")";
        newRoom.transform.GetChild(1).gameObject.name = count.ToString();
        newRoom.transform.GetChild(2).GetComponent<Button>().name = name; // 입장 버튼 이름을 방 이름으로 설정
        newRoom.SetActive(true);

        roomDict.Add(name, newRoom);
    }
    #endregion

    #region 방 입장
    public void OnClickJoin()
    {
        PhotonNetwork.JoinRoom(EventSystem.current.currentSelectedGameObject.name);
    }
    #endregion
}
