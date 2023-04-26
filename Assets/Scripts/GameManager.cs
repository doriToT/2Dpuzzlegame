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
    // ���ۺκ�
    public GameObject donglePrefab;
    public Transform dongleGroup;
    public List<Dongle> donglePool;
    // ����Ʈ �κ�
    public GameObject effectPrefab;
    public Transform effectGroup;
    public List<ParticleSystem> effectPool;
    // ������Ʈ Ǯ�� �κ�
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

    // Interpolate: ���� �������� ���Ͽ� �������� �ε巴�� ����.

    public void GameStart()
    {
        // ������Ʈ Ȱ��ȭ
        line.SetActive(true);
        bottom.SetActive(true);
        scoreText.gameObject.SetActive(true);
        maxScoreText.gameObject.SetActive(true);
        startGroup.SetActive(false);

        // ���� �÷���
        bgmPlayer.Play();
        SfxPlay(Sfx.Button);

        // ���� ����(���ۻ���)
        Invoke("NextDongle", 1.5f); 
    }

    Dongle MakeDongle()
    {
        // ����Ʈ ����
        GameObject instantEffectObj = Instantiate(effectPrefab, effectGroup);
        instantEffectObj.name = "Effect " + effectPool.Count;
        ParticleSystem instantEffect = instantEffectObj.GetComponent<ParticleSystem>();
        effectPool.Add(instantEffect); 

        // ���ۻ���                                                �θ�
        GameObject instantDongleObj = Instantiate(donglePrefab, dongleGroup);
        instantDongleObj.name = "Dongle " + donglePool.Count;
        Dongle instantDongle = instantDongleObj.GetComponent<Dongle>();

        // ���� �����ϸ鼭 �ٷ� manager, effect ������ �����ߴ� ������ �ʱ�ȭ
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
            if(!donglePool[poolCursor].gameObject.activeSelf)   // ActiveSelf: bool������ ������Ʈ Ȱ��ȭ �Ӽ�
            {
                return donglePool[poolCursor]; // Ž���Ͽ� ���� ������Ʈ�� ��Ȱ��ȭ��� return���� ��ȯ
            }
        }
        
        return MakeDongle();   // �ش��Լ������� ��(instantDongle)�� ��ȯ�ǰ� �����Ƿ� �Լ� ��ȯ ����!
    }


    void NextDongle()
    {
        // ���ӿ��� ������ ����� ���� ���ۻ��� ����.
        if(isOver)
        {
            return;
        }

        // ���� ������ ������ ������ �����ϱ�.
        lastDongle = GetDongle();
        lastDongle.level = Random.Range(0, maxLevel);
        lastDongle.gameObject.SetActive(true);

        SfxPlay(Sfx.Next);
        StartCoroutine("WaitNext");
    }

    // �ڷ�ƾ(Coroutine): ���� ��� ����Ƽ���� �ñ�� �Լ�
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
        // 1. ��� �ȿ� Ȱ��ȭ �Ǿ��ִ� ��� ���� ��������
        Dongle[] dongles = GameObject.FindObjectsOfType<Dongle>();

        // 2. ����� ���� ��� ������ ����ȿ�� ��Ȱ��ȭ
        for (int index = 0; index < dongles.Length; index++)
        {
            dongles[index].rigid.simulated = false;     // ���ӿ����� �� �ϰ� ����
        }

        // 3. 1���� ����� �ϳ��� �����ؼ� �����
        for (int index = 0; index < dongles.Length; index++)
        {
            dongles[index].Hide(Vector3.up * 100);
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(1f);

        // �ְ� ���� ����
        // Mathf.Max �Լ��� ���� ������ ����� ���� �� �ִ밪�� ����
        int maxScore = Mathf.Max(score, PlayerPrefs.GetInt("MaxSocre"));
        PlayerPrefs.SetInt("MaxScore", maxScore);
        // ���ӿ��� UI ǥ��
        subScoreText.text = "����: " + scoreText.text;
        endGroup.SetActive(true);

        bgmPlayer.Stop();
        SfxPlay(Sfx.Over);
    }

    // ���� ������� ���� �Լ�
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

    // ����Ͽ��� ������ ����� ���� Update���� ���� �߰�
    void Update()
    {
        if(Input.GetButtonDown("Cancel"))
        {
            Application.Quit();
        }
    }
    void LateUpdate()
    {
        scoreText.text = score.ToString();    // ToString: ���ڿ� Ÿ������ ��ȯ���ִ� �Լ�
    }
}

// ������ƮǮ��(ObjectPooling): �̸� �����ص� ������Ʈ ��Ȱ�� -> ���� ������ �����ؾ��Ѵ�.
// FindObjectsOfType<T> : ��鿡 �ö�� T ������Ʈ���� Ž��

// Rigidbody2D���� Sleeping Mode: ���������� ���߰� ���� ���¸�� ����

// ����Ʈ ��ƼŬ �ý���
// Limited Velocity over LifeTime: �ð��� ���� �ӵ� ����
// Trails: ���ڿ� ���� Ȥ�� ����ȿ�� �߰�