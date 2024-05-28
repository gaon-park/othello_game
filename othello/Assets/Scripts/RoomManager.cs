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

    #region �г�
    public GameObject lobbyPanel;
    public GameObject roomPanel;
    #endregion

    #region ���� ����Ʈ
    public Transform userList;
    public GameObject userTemplate;
    #endregion

    #region ä�� ���
    public Transform chatScrollContent;
    public GameObject chatTemplate;
    #endregion

    #region ä�� �Է�
    public TMP_InputField inputField;
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
        userTemplate.SetActive(false);

        readyButton.onClick.AddListener(OnClickReady);
        leaveButton.onClick.AddListener(OnClickLeft);

    }

    private void Update()
    {
        // ä�� �Է�
        if (!inputField.isFocused && (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return)))
        {
            if (!inputField.text.IsNullOrEmpty())
            {
                photonView.RPC("AddChatHistory", RpcTarget.All, PhotonNetwork.LocalPlayer.NickName + ": " + inputField.text);
                inputField.text = "";
            }
        }
    }

    #region �غ�
    public void OnClickReady()
    {

    }

    #endregion

    #region �� ����/����
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        lobbyPanel.SetActive(false);
        roomPanel.SetActive(true);

        // ���� ����Ʈ �ʱ�ȭ
        for (int i = 0; i < userList.childCount; i++)
        {
            if (userList.GetChild(i).gameObject.activeSelf)
                Destroy(userList.GetChild(i).gameObject);
        }

        // ä�� �ʱ�ȭ
        for (int i = 0; i < chatScrollContent.childCount; i++)
        {
            if (chatScrollContent.GetChild(i).gameObject.activeSelf)
                Destroy(chatScrollContent.GetChild(i).gameObject);
        }

        // �Է� ���� �ʱ�ȭ
        inputField.text = "";

        // �ʱ⿡ ���� �ִ� ����� �ִٸ�?
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
        GameObject obj = Instantiate(chatTemplate, chatScrollContent);
        obj.GetComponent<TMP_Text>().text = msg;
        obj.SetActive(true);
    }

    #endregion
}
