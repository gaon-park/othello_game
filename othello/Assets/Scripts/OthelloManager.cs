using Photon.Pun;
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
}
