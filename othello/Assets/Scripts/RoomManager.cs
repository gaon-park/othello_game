using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager instance { get; private set; }

    #region �г�
    public GameObject lobbyPanel;
    public GameObject roomPanel;
    public GameObject othelloPanel;
    #endregion

    #region ���� ����Ʈ
    public Transform userList;
    public GameObject userTemplate;
    #endregion

    #region ä�� ���
    public Transform gameChatScrollContent;
    public GameObject gameChatTemplate;

    public Transform roomChatScrollContent;
    public GameObject roomChatTemplate;

    #endregion

    #region ä�� �Է�
    public TMP_InputField roomInputField;
    public TMP_InputField gameInputField;
    #endregion

    #region UI �Է�
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
        othelloPanel.SetActive(false);
        userTemplate.SetActive(false);
        roomChatTemplate.SetActive(false);
        gameChatTemplate.SetActive(false);

        readyButton.onClick.AddListener(OnClickReady);
        leaveButton.onClick.AddListener(OnClickLeft);
    }

    private void Update()
    {
        // ä�� �Է�
        if (!roomInputField.isFocused && (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return)))
        {
            if (!roomInputField.text.IsNullOrEmpty())
            {
                photonView.RPC("AddChatHistory", RpcTarget.All, PhotonNetwork.LocalPlayer.NickName + ": " + roomInputField.text);
                roomInputField.text = "";
            }
        }
    }

    #region �غ�
    public void OnClickReady()
    {
        ExitGames.Client.Photon.Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
        int[] readyArray = properties["ready"] as int[];
        List<int> readyList = new(readyArray);

        // ������ Ŭ���̾�Ʈ: ����
        if (PhotonNetwork.IsMasterClient)
        {
            // ������ �̿ܿ� ��� �غ� �Ϸ��� ���� �÷���
            if (readyList.Count + 1 == PhotonNetwork.CurrentRoom.PlayerCount)
            {
                // todo ĳ���� ���� ���� ���
                // todo 

                photonView.RPC("GameStart", RpcTarget.All);
            }
        }
        // �׿�: �غ�
        else
        {
            if (readyList.Contains(PhotonNetwork.LocalPlayer.ActorNumber)) readyList.Remove(PhotonNetwork.LocalPlayer.ActorNumber);
            else readyList.Add(PhotonNetwork.LocalPlayer.ActorNumber);

            properties["ready"] = readyList.ToArray();
            PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);
        
        // ����� ������Ƽ
        foreach (DictionaryEntry entry in propertiesThatChanged)
        {
            Debug.Log($"Room property {entry.Key} changed to {entry.Value}");
            
            // �غ� �Ϸ� ��ũ ����
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
    private void GameStart()
    {
        roomPanel.SetActive(false);
        othelloPanel.SetActive(true);
    }

    #endregion

    #region �� ����/����
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        lobbyPanel.SetActive(false);
        roomPanel.SetActive(true);
        othelloPanel.SetActive(false);

        // ���� ����Ʈ �ʱ�ȭ
        for (int i = 0; i < userList.childCount; i++)
        {
            if (userList.GetChild(i).gameObject.activeSelf)
                Destroy(userList.GetChild(i).gameObject);
        }

        // ä�� �ʱ�ȭ
        for (int i = 0; i < roomChatScrollContent.childCount; i++)
        {
            if (roomChatScrollContent.GetChild(i).gameObject.activeSelf)
                Destroy(roomChatScrollContent.GetChild(i).gameObject);
        }

        // �Է� ���� �ʱ�ȭ
        roomInputField.text = "";

        // �ʱ⿡ ���� �ִ� ����� �ִٸ�?
        foreach (Player p in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (p.IsLocal) continue;
            GameObject obj = Instantiate(userTemplate, userList);
            obj.name = p.ActorNumber.ToString(); // ������Ʈ �̸��� ActorNumber �� ����
            obj.transform.GetChild(0).GetComponent<TMP_Text>().text = p.NickName;
            obj.transform.GetChild(1).gameObject.SetActive(false); // �غ� �Ϸ�
            obj.SetActive(true);
        }

        OnPlayerEnteredRoom(PhotonNetwork.LocalPlayer);

        // �� Ŀ���� ������Ƽ �ʱ�ȭ
        if (PhotonNetwork.IsMasterClient)
        {
            ExitGames.Client.Photon.Hashtable properties = new()
            {
                { "ready", new int[0] } // value: actor number
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
        }
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
        // �� ������Ƽ���� �غ� ���� �����
        ExitGames.Client.Photon.Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
        int[] readyArray = properties["ready"] as int[];
        List<int> readyList = new(readyArray);
        if (readyList.Contains(PhotonNetwork.LocalPlayer.ActorNumber)) readyList.Remove(PhotonNetwork.LocalPlayer.ActorNumber);
        properties["ready"] = readyList.ToArray();
        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);

        // ���� ������ Ŭ���̾�Ʈ��� �� ����
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
        obj.name = newPlayer.ActorNumber.ToString(); // ������Ʈ �̸��� ActorNumber �� ����
        obj.transform.GetChild(0).GetComponent<TMP_Text>().text = newPlayer.NickName;
        obj.transform.GetChild(1).gameObject.SetActive(false); // �غ� �Ϸ�
        obj.SetActive(true);

        // ���� �˸�
        AddChatHistory("'" + newPlayer.NickName + "'" + "���� �����߽��ϴ�.");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        for (int i = 0; i < userList.childCount; i++)
        {
            GameObject obj = userList.GetChild(i).gameObject;
            if (!obj.activeSelf) continue;
            // ��� ������ ����� �г����� �Ȱ��ٸ�
            if (obj.transform.GetChild(0).GetComponent<TMP_Text>().text.Equals(otherPlayer.NickName))
            {
                Destroy(obj);
                break;
            }
        }

        // ���� �˸�
        AddChatHistory("'" + otherPlayer.NickName + "'" + "���� �����߽��ϴ�.");
    }
    #endregion

    #region ä��
    [PunRPC]
    private void AddChatHistory(string msg)
    {
        GameObject obj = Instantiate(gameChatTemplate, roomChatScrollContent);
        obj.GetComponent<TMP_Text>().text = msg;
        obj.SetActive(true);
    }

    #endregion
}
