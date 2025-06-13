using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    
    public CinemachineCamera tpsCamera;
    
    // 에디터에서 캐릭터 GameObject들을 할당하세요.
    public PlayerController[] characters;
    public PlayerController curPlayer;

    // 현재 활성화된 캐릭터 인덱스
    private int currentIndex = 0;

    public GameObject deathPopup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        
        // 시작 시 업그레이드/골드 데이터 로드
        UpgradeManager.LoadData();
    }

    // 게임 시작 시 첫 번째 캐릭터만 활성화하고 나머지는 비활성화
    void Start()
    {
        if (characters.Length == 0)
        {
            Debug.LogWarning("캐릭터 배열에 아무것도 할당되지 않았습니다.");
            return;
        }

        // 모든 캐릭터를 순회하며 첫 번째만 활성화, 나머지는 비활성화
        for (int i = 0; i < characters.Length; i++)
        {
            characters[i].gameObject.SetActive(i == currentIndex);
        }
    }

    // UI 버튼 OnClick() 이벤트에 연결하여 사용하세요.
    public void SwitchCharacter(int characterIndex)
    {
        if (characters.Length == 0)
        {
            Debug.LogWarning("캐릭터 배열에 아무것도 할당되지 않았습니다.");
            return;
        }

        // 현재 활성화된 캐릭터 비활성화
        var pos = characters[currentIndex].transform.position;
        var rot = characters[currentIndex].transform.rotation;
        characters[currentIndex].gameObject.SetActive(false);

        // 다음 캐릭터 활성화
        characters[characterIndex].transform.position = pos;
        characters[characterIndex].transform.rotation = rot;
        characters[characterIndex].LookAtTarget.position = characters[currentIndex].LookAtTarget.position;
        characters[characterIndex].LookAtTarget.rotation = characters[currentIndex].LookAtTarget.rotation;
        characters[characterIndex]._health = characters[currentIndex]._health;
        characters[characterIndex].gameObject.SetActive(true);

        tpsCamera.Follow = characters[characterIndex].transform;
        tpsCamera.LookAt = characters[characterIndex].LookAtTarget;
        
        // 스포너에게 알리기
        EnemySpawner.Instance.SetPlayer(characters[characterIndex]);

        currentIndex = characterIndex;
        
        // 현재 캐릭터 설정
        curPlayer = characters[characterIndex];
    }
    
    public void OnBackDashButton()
    {
        curPlayer.OnBackDashButton();
    }

    public void Die()
    {
        Time.timeScale = 0f;
        
        UIManager.Instance.ShowUpgradePopup();
    }
    
    public void RestartScene()
    {
        Time.timeScale = 1f;
        
        // 현재 활성화된 씬의 인덱스로 씬 재시작
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}