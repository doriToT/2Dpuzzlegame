using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("------------[ Main Core ]")]
    public bool isOver;
    public int score;
    public int maxLevel;

    [Header("------------[ Object Pooling ]")]
    // 동글부분
    public GameObject donglePrefab;
    public Transform dongleGroup;
    public List<Dongle> donglePool;
    // 이펙트 부분
    public GameObject effectPrefab;
    public Transform effectGroup;
    public List<ParticleSystem> effectPool;
    // 오브젝트 풀링 부분
    [Range(1, 30)]
    public int poolSize;
    public int poolCursor;
    public Dongle lastDongle;

    [Header("------------[ Audio ]")]
    public AudioSource bgmPlayer;
    public AudioSource[] sfxPlayer;
    public AudioClip[] sfxClip;
    public enum Sfx { LevelUp, Next, Attach, Button, Over };
    int sfxCursor;

    [Header("------------[ UI ]")]
    public GameObject startGroup;
    public GameObject endGroup;
    public Text scoreText;
    public Text maxScoreText;
    public Text subScoreText;

    [Header("------------[ ETC ]")]
    public GameObject line;
    public GameObject bottom;


    void Awake()
    {
        Application.targetFrameRate = 60;

        donglePool = new List<Dongle>();
        effectPool = new List<ParticleSystem>();
        for(int index = 0; index < poolSize; index++)
        {
            MakeDongle();
        }

        if(!PlayerPrefs.HasKey("MaxScore"))  
        {
            PlayerPrefs.SetInt("MaxScore", 0);
        }

        maxScoreText.text = PlayerPrefs.GetInt("MaxScore").ToString();   
    }

    // Interpolate: 이전 프레임을 비교하여 움직임을 부드럽게 보정.

    public void GameStart()
    {
        // 오브젝트 활성화
        line.SetActive(true);
        bottom.SetActive(true);
        scoreText.gameObject.SetActive(true);
        maxScoreText.gameObject.SetActive(true);
        startGroup.SetActive(false);

        // 사운드 플레이
        bgmPlayer.Play();
        SfxPlay(Sfx.Button);

        // 게임 시작(동글생성)
        Invoke("NextDongle", 1.5f); 
    }

    Dongle MakeDongle()
    {
        // 이펙트 생성
        GameObject instantEffectObj = Instantiate(effectPrefab, effectGroup);
        instantEffectObj.name = "Effect " + effectPool.Count;
        ParticleSystem instantEffect = instantEffectObj.GetComponent<ParticleSystem>();
        effectPool.Add(instantEffect); 

        // 동글생성                                                부모
        GameObject instantDongleObj = Instantiate(donglePrefab, dongleGroup);
        instantDongleObj.name = "Dongle " + donglePool.Count;
        Dongle instantDongle = instantDongleObj.GetComponent<Dongle>();

        // 동글 생성하면서 바로 manager, effect 변수를 생성했던 것으로 초기화
        instantDongle.manager = this; 
        instantDongle.effect = instantEffect;
        donglePool.Add(instantDongle);

        return instantDongle;
    }

    Dongle GetDongle()
    {
        for(int index = 0; index < donglePool.Count; index++)
        {
            poolCursor = (poolCursor + 1) % donglePool.Count;
            if(!donglePool[poolCursor].gameObject.activeSelf)   // ActiveSelf: bool형태의 오브젝트 활성화 속성
            {
                return donglePool[poolCursor]; // 탐색하여 만난 오브젝트가 비활성화라면 return으로 반환
            }
        }
        
        return MakeDongle();   // 해당함수에서도 값(instantDongle)을 반환되고 있으므로 함수 반환 가능!
    }


    void NextDongle()
    {
        // 게임오버 변수를 사용해 다음 동글생성 막기.
        if(isOver)
        {
            return;
        }

        // 새로 생성된 동글을 변수에 저장하기.
        lastDongle = GetDongle();
        lastDongle.level = Random.Range(0, maxLevel);
        lastDongle.gameObject.SetActive(true);

        SfxPlay(Sfx.Next);
        StartCoroutine("WaitNext");
    }

    // 코루틴(Coroutine): 로직 제어를 유니티에게 맡기는 함수
    IEnumerator WaitNext()
    {
        while (lastDongle != null)
        {
            yield return null;
        }

        yield return new WaitForSeconds(2.5f);

        NextDongle();
    }

    public void TouchDown()
    {
        if (lastDongle == null)
            return;

        lastDongle.Drag();
    }

    public void TouchUp()
    {
        if (lastDongle == null)
            return;

        lastDongle.Drop();
        lastDongle = null;
    }

    public void GameOver()
    {
        if(isOver)
        {
            return;
        }

        isOver = true;

        StartCoroutine("GameOverRoutine");
    }

    IEnumerator GameOverRoutine()
    {
        // 1. 장면 안에 활성화 되어있는 모든 동글 가져오기
        Dongle[] dongles = GameObject.FindObjectsOfType<Dongle>();

        // 2. 지우기 전에 모든 동글의 물리효과 비활성화
        for (int index = 0; index < dongles.Length; index++)
        {
            dongles[index].rigid.simulated = false;     // 게임오버일 때 일괄 적용
        }

        // 3. 1번의 목록을 하나씩 접근해서 지우기
        for (int index = 0; index < dongles.Length; index++)
        {
            dongles[index].Hide(Vector3.up * 100);
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(1f);

        // 최고 점수 갱신
        // Mathf.Max 함수로 현재 점수와 저장된 점수 중 최대값을 저장
        int maxScore = Mathf.Max(score, PlayerPrefs.GetInt("MaxSocre"));
        PlayerPrefs.SetInt("MaxScore", maxScore);
        // 게임오버 UI 표시
        subScoreText.text = "점수: " + scoreText.text;
        endGroup.SetActive(true);

        bgmPlayer.Stop();
        SfxPlay(Sfx.Over);
    }

    // 게임 재시작을 위한 함수
    public void Reset()
    {
        SfxPlay(Sfx.Button);
        StartCoroutine("ResetCoroutine");
    }

    IEnumerator ResetCoroutine()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("Main");
    }

    public void SfxPlay(Sfx type)
    {
        switch(type)
        {
            case Sfx.LevelUp:
                sfxPlayer[sfxCursor].clip = sfxClip[Random.Range(0, 3)];
                break;
            case Sfx.Next:
                sfxPlayer[sfxCursor].clip = sfxClip[3];
                break;
            case Sfx.Attach:
                sfxPlayer[sfxCursor].clip = sfxClip[4];
                break;
            case Sfx.Button:
                sfxPlayer[sfxCursor].clip = sfxClip[5];
                break;
            case Sfx.Over:
                sfxPlayer[sfxCursor].clip = sfxClip[6];
                break;
        }

        sfxPlayer[sfxCursor].Play();
        sfxCursor = (sfxCursor + 1) % sfxPlayer.Length;
    }

    // 모바일에서 나가는 기능을 위해 Update에서 로직 추가
    void Update()
    {
        if(Input.GetButtonDown("Cancel"))
        {
            Application.Quit();
        }
    }
    void LateUpdate()
    {
        scoreText.text = score.ToString();    // ToString: 문자열 타입으로 변환해주는 함수
    }
}

// 오브젝트풀링(ObjectPooling): 미리 생성해둔 오브젝트 재활용 -> 재사용 로직을 구성해야한다.
// FindObjectsOfType<T> : 장면에 올라온 T 컴포넌트들을 탐색

// Rigidbody2D에서 Sleeping Mode: 물리연산을 멈추고 쉬는 상태모드 설정

// 이펙트 파티클 시스템
// Limited Velocity over LifeTime: 시간에 따른 속도 제어
// Trails: 입자에 꼬리 혹은 리본효과 추가