using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

// 단일 테트리스 
public class Stage : MonoBehaviour
{
    //받아와야하는 보드위에 있는 것, 배경, 테트로미노
    [Header("BoardArea")]
    public GameObject tilePrefab;
    public Transform[] Board;
    public Transform[] Background;
    public Transform[] Tetromino;

    // 다음 블록
    private LinkedList<int> List;
    private LinkedListNode<int> linkedListNode;

    // 이걸 배열..? 로 해서 한번에 for문같은걸로 관리해보기
    [Header("UI")]
    public Text[] BlocksText;
    public Image[] NextBlocksImage;
    public Sprite[] BlockSprites;
    public Text timeText;
    public Text ScoreText;
    public AudioClip BGM;
    public AudioSource audio;
    public Slider bgmslider;
    // 변수
    private int Width;
    private int Height;
    private int halfWidth;
    private int halfHeight;
    private Vector2 player1Position; // 플레이어 1 기준 위치
    private Vector2 player2Position; // 플레이어 2 기준 위치
    private float curTime; // 현재 시간
    private float fallTime; // 떨어지는 주기
    private float BGMtimer = 0.1f;


    private float time;

    private float timer;
    private int CurColor;

    private string blockText;
    private Color32 textColor;
    private Sprite blockSprite;
    private int Score;

    private bool isPlaying;

    GameObject obj;

    private Byte[] buffer = new byte[1024];
    private bool FIRST =true;
    private void Start()
    {
        isPlaying = true;

        this.audio = this.gameObject.AddComponent<AudioSource>();
        this.audio.loop = false;
        this.audio.volume = 0.3f;

        blockText = "0";
        //가로 세로 길이
        Width = 10;
        Height = 20;
        halfWidth = Mathf.RoundToInt(Width * 0.5f);
        halfHeight = Mathf.RoundToInt(Height * 0.5f);

        //떨어지는 시간
        curTime = 0.0f;
        fallTime = 1.0f;

        timer = 0.0f;

        // 점수
        Score = 0;
        // 테트리스 판의 전체적인 위치 옮기기
        player1Position = new Vector2(-10, 0);
        Board[0].position = player1Position;
        Background[0].position = player1Position;
        player2Position = new Vector2(10, 0);
        Board[1].position = player2Position;
        Background[1].position = player2Position;


        //다음 뭘 테트로미노가 뭐일지 링크드 리스트
        List = new LinkedList<int>();
        linkedListNode = List.First;
        for (int i = 0; i < 4; i++)
        {
            List.AddLast(UnityEngine.Random.Range(0, 7));
        }


        
    }

    private void Update()
    {
        if(FIRST)
        {
            Example.SendPacket.color = List.First.Value;
            GameObject.Find("TestObject").GetComponent<Example>().SendPackect(Example.SendPacket);
            GameObject.Find("TestObject").GetComponent<Example>().RecvMessageStruct(buffer);
            CreatePlayer1Area();
            CreatePlayer2Area();
            FIRST = false;
        }
        audio.volume = bgmslider.value;
        if (isPlaying) //
        {
            BGMtimer -= Time.deltaTime;
            if (this.audio.isPlaying == false && BGMtimer <= 0)
            {
                this.audio.clip = this.BGM;
                this.audio.Play();
                BGMtimer = 5;
            }

            Vector3 moveDir = Vector3.zero;
            bool isRotate = false;


            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                moveDir.x = -1;
                Example.SendPacket.move = 0;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                moveDir.x = 1;
                Example.SendPacket.move = 1;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                isRotate = true;
                Example.SendPacket.move = 4;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                moveDir.y = -1;
                Example.SendPacket.move = 2;
                Score++;
                ScoreText.text = Score.ToString();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                while (MoveTetromino(Vector3.down, false,0))
                {
                }
                Example.SendPacket.move = 3;
                Example.move = true;
                Score +=20;
                ScoreText.text = Score.ToString();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Tetris");
            }

            curTime += Time.deltaTime;
            if (curTime > fallTime)
            {
                moveDir.y = -1;
                curTime = 0;
                isRotate = false;
                Example.SendPacket.move = 5;
            }


            if (moveDir != Vector3.zero || isRotate)
            {
                if(MoveTetromino(moveDir, isRotate, 0))
                {
                    //여기서 패킷을 보낸다는 걸 알려줌
                    Example.move = true;
                }
            }
            
            if (Example.moveDir2 != Vector3.zero || Example.isRotate2)
            {
                MoveTetromino(Example.moveDir2, Example.isRotate2, 1);
                Example.moveDir2 = Vector3.zero;
                Example.isRotate2 = false;
            }

            //Timer
            time += Time.deltaTime;
            timer += Time.deltaTime;
            timeText.text = ((int)time).ToString();
        }
    }

    // 플레이어부분 만들기
    void CreatePlayer1Area()
    {
        CreateBackGround(0);
        CreateBoard(0);
        CreateTetromino(0);
    }

    // 상대방부분 만들기
    void CreatePlayer2Area()
    {
        CreateBackGround(1);
        CreateBoard(1);
        CreateTetromino(1);
    }

    void CreateBoard(int num)
    {
        for (int i = 0; i < Height; i++)
        {
            var board = new GameObject(i.ToString());
            board.transform.parent = Board[num];
            board.transform.position = new Vector3(0, i - halfHeight, 0) + Board[num].position;
        }
    }

    // 배경 만들기
    void CreateBackGround(int num)
    {
        Color32 color = Color.gray;

        //타일보드 (게임진행되는곳)
        for(int i = -halfWidth; i < halfWidth; i++)
        {
            for(int j = -halfHeight; j < halfHeight; j++)
            {
                CreateTile(Background[num], new Vector2(i, j) , color, 0);
            }
        }

        color = Color.white;
        //아래테두리
        for (int i = -halfWidth; i < halfWidth; i++)
        {
            CreateTile(Background[num], new Vector2(i, -halfHeight -1) , color, 0);
        }
        //양쪽테두리
        for (int i = -halfHeight-1; i < halfHeight; i++)
        {
            CreateTile(Background[num], new Vector2(-halfWidth - 1, i) , color, 0);
            CreateTile(Background[num], new Vector2(halfWidth, i) , color, 0);
        }
    }

    // 타일 생성하기 (부모 지정, 위치 지정, 색깔 지정, 순서지정)
    void CreateTile(Transform parent, Vector2 position ,Color color, int order)
    {
        var tile  = Instantiate(tilePrefab);
        tile.transform.parent = parent;
        tile.transform.localPosition = position;
        
        var Color = tile.GetComponent<Tile>();
        Color.color = color;
        Color.sortingOrder = order;
    }
    
    // 테트로미노 만들기
    public void CreateTetromino(int num)
    {
        if(num==0)
        {
            //리스트의 처음 값으로 만들고 지우기
            int listNumber = List.First.Value;
            Example.SendPacket.color = listNumber;
            CurColor = listNumber;
            List.RemoveFirst();

            Color32 color = Color.black;

            Tetromino[num].transform.position = new Vector2(0.0f, halfHeight) + player1Position;
            Tetromino[num].rotation = Quaternion.identity;

            switch (listNumber)
            {
                //하늘색
                case 0:
                    color = new Color32(115, 251, 253, 255);
                    CreateTile(Tetromino[num], new Vector2(-2.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(1.0f, 0.0f), color, 1);
                    break;
                //파란색
                case 1:
                    color = new Color32(0, 33, 245, 255);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(1.0f, 0.0f), color, 1);
                    break;
                //주황색
                case 2:
                    color = new Color32(243, 168, 59, 255);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(1.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(1.0f, 1.0f), color, 1);
                    break;
                //노란색
                case 3:
                    color = new Color32(255, 253, 84, 255);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 0.0f), color, 1);
                    break;
                //녹색
                case 4:
                    color = new Color32(117, 250, 76, 255);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(1.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 0.0f), color, 1);
                    break;
                //보라색
                case 5:
                    color = new Color32(155, 47, 246, 255);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(1.0f, 0.0f), color, 1);
                    break;
                //빨강색
                case 6:
                    color = new Color32(235, 51, 35, 255);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(1.0f, 0.0f), color, 1);
                    break;
            }


            //리스트의 마지막에 값 추가
            List.AddLast(UnityEngine.Random.Range(0, 7));
            NextBlockLists();
        }
        else if(num==1)
        {
            Tetromino[num].transform.position = new Vector2(0.0f, halfHeight) + player2Position;
            Tetromino[num].rotation = Quaternion.identity;

            Color32 color = Color.black;
            
            switch (Example.RecvPacket.color)
            {
                //하늘색
                case 0:
                    color = new Color32(115, 251, 253, 255);
                    CreateTile(Tetromino[num], new Vector2(-2.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(1.0f, 0.0f), color, 1);
                    break;
                //파란색
                case 1:
                    color = new Color32(0, 33, 245, 255);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(1.0f, 0.0f), color, 1);
                    break;
                //주황색
                case 2:
                    color = new Color32(243, 168, 59, 255);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(1.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(1.0f, 1.0f), color, 1);
                    break;
                //노란색
                case 3:
                    color = new Color32(255, 253, 84, 255);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 0.0f), color, 1);
                    break;
                //녹색
                case 4:
                    color = new Color32(117, 250, 76, 255);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(1.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 0.0f), color, 1);
                    break;
                //보라색
                case 5:
                    color = new Color32(155, 47, 246, 255);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(1.0f, 0.0f), color, 1);
                    break;
                //빨강색
                case 6:
                    color = new Color32(235, 51, 35, 255);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(0.0f, 0.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(-1.0f, 1.0f), color, 1);
                    CreateTile(Tetromino[num], new Vector2(1.0f, 0.0f), color, 1);
                    break;
            }
        }
    }

    public bool MoveTetromino(Vector3 moveDir, bool isRotate, int num)
    {
        Vector3 oldPos = Tetromino[num].transform.position;
        Quaternion oldRot = Tetromino[num].transform.rotation;

        Tetromino[num].transform.position += moveDir;
        if (isRotate)
        {
            Tetromino[num].transform.rotation *= Quaternion.Euler(0, 0, 90);
        }

        if (!isCanMove(num))
        {
            Tetromino[num].transform.position = oldPos;
            Tetromino[num].transform.rotation = oldRot;

            if ((int)moveDir.y == -1 && (int)moveDir.x == 0 && isRotate == false)
            {
                if(num==0)
                {
                    Example.move = true;
                }
                
                AddBoard(num);
                CheckRow(num);
                CreateTetromino(num);

                if (!isCanMove(num))
                {
                    Debug.Log("게임오버");
                    isPlaying = false;
                }
            }

            return false;
        }

        return true;
    }

    bool isCanMove(int num)
    {
        //범위 밖으로 나감 (못움직임)
        if(num==0)
        {
            for (int i = 0; i < Tetromino[num].childCount; i++) // 테트로노미노 개수만큼 확인
            {
                var Tetrominochild = Tetromino[num].GetChild(i);
                // x 범위
                int x = Mathf.RoundToInt(Tetrominochild.transform.position.x + Width);
                if (x < -halfWidth || x > halfWidth - 1)
                    return false;
                // y 범위
                int y = Mathf.RoundToInt(Tetrominochild.transform.position.y + halfHeight);
                if (y < 0)
                    return false;

                //블럭이 쌓였을 때
                var col = Board[num].Find(y.ToString());
                if (col != null && col.Find((x + halfWidth).ToString()) != null)
                    return false;
            }
            //범위 안에 있음 (움직일수 있음)
            return true;
        }
        else 
        {
            for (int i = 0; i < Tetromino[num].childCount; i++) // 테트로노미노 개수만큼 확인
            {
                var Tetrominochild = Tetromino[num].GetChild(i);
                // x 범위 5~14
                int x = Mathf.RoundToInt(Tetrominochild.transform.position.x - Width);
                if (x < -halfWidth || x > halfWidth - 1) 
                    return false;
                // y 범위
                int y = Mathf.RoundToInt(Tetrominochild.transform.position.y + halfHeight);
                
                if (y < 0)
                    return false;

                //블럭이 쌓였을 때
                var col = Board[num].Find(y.ToString());
                if (col != null && col.Find((x + halfWidth).ToString()) != null)
                    return false;
            }
            //범위 안에 있음 (움직일수 있음)
            return true;
        }
    }

    void AddBoard(int num)
    {
        if(num==0)
        {
            while (Tetromino[num].childCount > 0)
            {
                // 테트로미노 자식 구하기
                var Tetrominochild = Tetromino[num].GetChild(0);

                int x = Mathf.RoundToInt(Tetrominochild.position.x + Width + halfWidth); // 얘가 몇번째 그 가로줄에서 왼쪽에서몇번째인지
                int y = Mathf.RoundToInt(Tetrominochild.position.y + halfHeight); //얘가 몇번쨰 세로줄인지

                // Board에 있는 y에 x라는 이름으로 넣기
                Tetrominochild.parent = Board[num].Find(y.ToString());
                Tetrominochild.name = x.ToString();
            }
        }
        else if(num==1)
        {
            while (Tetromino[num].childCount > 0)
            {
                // 테트로미노 자식 구하기
                var Tetrominochild = Tetromino[num].GetChild(0);

                int x = Mathf.RoundToInt(Tetrominochild.position.x + Width + halfWidth); // 얘가 몇번째 그 가로줄에서 왼쪽에서몇번째인지
                int y = Mathf.RoundToInt(Tetrominochild.position.y + halfHeight); //얘가 몇번쨰 세로줄인지

                // Board에 있는 y에 x라는 이름으로 넣기
                Tetrominochild.parent = Board[num].Find(y.ToString());
                Tetrominochild.name = (x-20).ToString();
            }
        }
        
    }

    //
    void CheckRow(int num)
    {
        bool isClean = false;

        int completecount = 0;

        // 완성된 행 == 행의 자식 갯수가 가로 크기
        foreach (Transform Row in Board[num])
        {
            if (Row.childCount == Width)
            {
                foreach (Transform tile in Row)
                {
                    Destroy(tile.gameObject);
                }
                Row.DetachChildren(); // 얘는 왜 필요하지..?
                isClean = true;
                completecount++;
            }
        }

        // 지워졌을때
        if (isClean)
        {
            //쓰기 힘듦 foreach(Transform Row in Board) {} -> for

            for (int i=0; i<Height; i++)
            {
                var Row = Board[num].Find(i.ToString());

                //비어있으면
                if (Row.childCount == 0)
                    continue;

                //빈 줄 개수
                int emptyRow = 0;
                //아래 줄 수
                int j = i - 1;
                //빈줄 수 구하기
                while (j >= 0)
                {
                    if (Board[num].Find(j.ToString()).childCount == 0)
                    {
                        emptyRow++;
                    }
                    j--;
                }
                // 빈줄이 없을 때까지
                if (emptyRow > 0)
                {
                    // 가야할 줄 위치
                    var togoRow = Board[num].Find((i - emptyRow).ToString());

                    // 줄의 블럭들이 모두 이동할 때 까지 반복 (보드에 옮길때랑 같음)
                    while (Row.childCount > 0)
                    {
                        var tile = Row.GetChild(0);
                        tile.parent = togoRow;
                        tile.transform.position += new Vector3(0, -emptyRow, 0);
                    }
                    Row.DetachChildren(); // 얘는 왜 필요하지..?
                }
            }
        }

        // 완성된 줄에 따른 점수 부여
        switch(completecount)
        {
            case 1:
                Score += 100;
                break;
            case 2:
                Score += 300;
                break;
            case 3:
                Score += 500;
                break;
            case 4:
                Score += 1000;
                break;
        }
        ScoreText.text = Score.ToString();
    }
    
    void NextBlockLists()
    {
        linkedListNode = List.First;
        for (int i = 0; i < 3; i++)
        {
            CheckColor(linkedListNode.Value);
            BlocksText[i].text = blockText;
            BlocksText[i].color = textColor;
            NextBlocksImage[i].sprite = blockSprite;
            linkedListNode = linkedListNode.Next;
        }
    }

    void CheckBlockList()
    {
        linkedListNode = List.First;
        for(int i=0; i<3; i++)
        {
            CheckColor(linkedListNode.Value);
            BlocksText[i].text = blockText;
            BlocksText[i].color = textColor;
            linkedListNode = linkedListNode.Next;
        }
    }

    //하늘색, 파란색, 주황색, 노란색, 녹색, 보라색, 빨강색
    void CheckColor(int num)
    {
        switch (num)
        {
            case 0:
                textColor = new Color32(115, 251, 253, 255);
                blockSprite = BlockSprites[0];
                blockText = "하늘색";
                break;
            case 1:
                textColor = new Color32(0, 33, 245, 255);
                blockSprite = BlockSprites[1];
                blockText = "파란색";
                break;
            case 2:
                textColor = new Color32(243, 168, 59, 255);
                blockSprite = BlockSprites[2];
                blockText = "주황색";
                break;
            case 3:
                textColor = new Color32(255, 253, 84, 255);
                blockSprite = BlockSprites[3];
                blockText = "노란색";
                break;
            case 4:
                textColor = new Color32(117, 250, 76, 255);
                blockSprite = BlockSprites[4];
                blockText = "녹색";
                break;
            case 5:
                textColor = new Color32(155, 47, 246, 255);
                blockSprite = BlockSprites[5];
                blockText = "보라색";
                break;
            case 6:
                textColor = new Color32(235, 51, 35, 255);
                blockSprite = BlockSprites[6];
                blockText = "빨강색";
                break;
            default:
                blockText = "X";
                break;
        }
        return;
    }
}