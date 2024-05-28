using Photon.Pun;
using Photon.Realtime;
using System.Collections;
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
    #endregion

    #region 유저 리스트
    public Transform userList;
    public GameObject userTemplate;
    #endregion

    #region 채팅 기록
    public Transform chatScrollContent;
    public GameObject chatTemplate;
    #endregion

    #region 채팅 입력
    public TMP_InputField inputField;
    #endregion

    #region UI 입력
    public Button readyButton;
    public Button leaveButton;
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
        userTemplate.SetActive(false);

        readyButton.onClick.AddListener(OnClickReady);
        leaveButton.onClick.AddListener(OnClickLeft);

    }

    private void Update()
    {
        // 채팅 입력
        if (!inputField.isFocused && (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return)))
        {
            if (!inputField.text.IsNullOrEmpty())
            {
                photonView.RPC("AddChatHistory", RpcTarget.All, PhotonNetwork.LocalPlayer.NickName + ": " + inputField.text);
                inputField.text = "";
            }
        }
    }

    #region 준비
    public void OnClickReady()
    {

    }

    #endregion

    #region 방 입장/퇴장
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        lobbyPanel.SetActive(false);
        roomPanel.SetActive(true);

        // 유저 리스트 초기화
        for (int i = 0; i < userList.childCount; i++)
        {
            if (userList.GetChild(i).gameObject.activeSelf)
                Destroy(userList.GetChild(i).gameObject);
        }

        // 채팅 초기화
        for (int i = 0; i < chatScrollContent.childCount; i++)
        {
            if (chatScrollContent.GetChild(i).gameObject.activeSelf)
                Destroy(chatScrollContent.GetChild(i).gameObject);
        }

        // 입력 상자 초기화
        inputField.text = "";

        // 초기에 들어와 있는 사람이 있다면?
        foreach (Player p in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (p.IsLocal) continue;
            GameObject obj = Instantiate(userTemplate, userList);
            obj.transform.GetChild(0).GetComponent<TMP_Text>().text = p.NickName;
            obj.SetActive(true);
        }

        OnPlayerEnteredRoom(PhotonNetwork.LocalPlayer);
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

        GameObject obj = Instantiate(userTemplate, userList);
        obj.transform.GetChild(0).GetComponent<TMP_Text>().text = newPlayer.NickName;
        obj.SetActive(true);

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
        GameObject obj = Instantiate(chatTemplate, chatScrollContent);
        obj.GetComponent<TMP_Text>().text = msg;
        obj.SetActive(true);
    }

    #endregion
}
