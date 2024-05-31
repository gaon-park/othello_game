## 프로젝트 설명

> 오델로(Othello) 게임
> 

바둑돌과 비슷한 흑백의 작은 원판을 가로세로 8줄로 이루어진 네모난 격자판 위에 늘어놓는 보드 게임으로, 1971년 하세가와 고로와 제임스 R. 베커가 영국의 루이스 워터맨이 1883년 고안한 보드 게임 '리버시(Reversi)'를 변형시켜 만든 게임이다. 어원은 세익스피어의 희곡 '오셀로'로 오셀로의 이중성 또는 오셀로와 데스데모나가 이루는 흑백의 대비를 모티브로 삼았다고 한다. 원판은 대개 흑백으로 되어있지만, 적청으로 만드는 경우도 있다.

참고: [https://namu.wiki/w/오델로(게임)](https://namu.wiki/w/%EC%98%A4%EB%8D%B8%EB%A1%9C(%EA%B2%8C%EC%9E%84))

## 개발 일지

| 작업 | 태그 | 완료 날짜 |
| --- | --- | --- |
| 포톤 네트워크 설정 | 로그인 | @2024년 5월 28일 |
| 닉네임 설정 후 로비 입장 | 로그인 | @2024년 5월 28일 |
| 방 생성 정보 실시간 동기화 | 로비 | @2024년 5월 28일 |
| 방 삭제 정보 실시간 동기화 | 로비 | @2024년 5월 28일 |
| 방 참여 인원 정보 실시간 동기화 | 로비 | @2024년 5월 28일 |
| 방 참여 인원 MAX 및 게임 시작에 따른 입장 제한 | 로비 |  |
| 관전 모드 입장 | 로비 |  |
| 각 플레이어 준비 정보 동기화 | 준비 | @2024년 5월 28일 |
| 방 입장 정보 동기화 | 준비 | @2024년 5월 28일 |
| 채팅 정보 동기화 | 준비 | @2024년 5월 28일 |
| 방 나가기 | 준비 | @2024년 5월 28일 |
| 오셀로 게임 UI | 게임 | @2024년 5월 29일 |
| 캐릭터 아이콘 랜덤 생성 | 준비 | @2024년 5월 29일 |
| [게임 로직](#게임-로직) | 게임 | @2024년 5월 31일 |
| [턴제 게임 로직](#턴제-게임-로직) | 게임 | @2024년 5월 31일 |
| 방장 UI | 준비 | @2024년 5월 31일 |

## 게임 로직

8방향으로 뻗어나가며 재귀적으로 현재 위치에 돌을 놓을 수 있는지 판별하는 알고리즘 작성

```csharp
private static readonly int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
private static readonly int[] dy = { 1, 1, 0, -1, -1, -1, 0, 1 };

#region 포지션 버튼
private static int BOARD_SIZE = 8;
private Button[,] boards = new Button[BOARD_SIZE, BOARD_SIZE];
private int[,] states = new int[BOARD_SIZE, BOARD_SIZE];
#endregion

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
```

위와 같은 로직으로 사용자가 한 위치에 돌을 놓았을 때, 그 위치에서 먹을 수 있는 상대방의 돌을 모두 내것으로 만듦

```csharp
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
```

## 턴제 게임 로직

모든 이벤트는 버튼 클릭으로 실행된다. 

처음 순서는 무조건 MasterClient부터이다. 

`turn` 이라는 ActorNumber 전역변수를 두어, RPC 함수로 동기화시키며 관리한다. 

이번 턴에서 내가 놓을 수 있는 위치가 없으면 그 즉시 상대방에게 턴을 넘기며, 

직전 유저가 돌을 놓을 곳이 없어 턴을 스킵했는데 이번 턴의 나 역시 놓을 곳이 없다면 게임을 종료한다. 

“방 나가기” 버튼으로 누군가 게임을 중간에 포기할 경우를 고려하여 `GameFinished()` 함수의 `leftPerson` 를 두어 관리한다. 

```csharp
private int turn = 0;
private List<int[]> currentInteractablePositionList = new();

public void GameStart()
{
		// 초기 세팅 로직 ...
    // 마스터부터 시작
    photonView.RPC("NextTurn", RpcTarget.All, PhotonNetwork.MasterClient.ActorNumber);
    SetEnablePosition(false); // 돌을 놓을 수 있는 위치 탐색 메소드
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
```