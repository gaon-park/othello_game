using UnityEngine;
using Photon.Pun;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using WebSocketSharp;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager instance { get; private set; }

    #region �α���
    public TMP_InputField nicknameField;
    public Button loginButton;
    #endregion

    #region �г�
    public GameObject loginPanel;
    public GameObject lobbyPanel;
    public GameObject roomPanel;
    #endregion

    public readonly int MAX_PLAYER = 2;

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
        PhotonNetwork.ConnectUsingSettings();
        loginPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(false);
    }

    void Update()
    {
        loginButton.interactable = PhotonNetwork.IsConnectedAndReady && !nicknameField.text.IsNullOrEmpty();
    }

    public void OnClickLogin()
    {
        // �κ� ���� ��û
        StartCoroutine(WaitAndJoinLobby());
    }

    public override void OnConnected()
    {
        base.OnConnected();
        print(System.Reflection.MethodBase.GetCurrentMethod().Name);
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        print(System.Reflection.MethodBase.GetCurrentMethod().Name);

        loginPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        print(System.Reflection.MethodBase.GetCurrentMethod().Name);
    }

    private IEnumerator WaitAndJoinLobby()
    {
        while (!PhotonNetwork.IsConnectedAndReady)
            yield return null;

        PhotonNetwork.LocalPlayer.NickName = nicknameField.text;
        PhotonNetwork.JoinLobby();
    }
}