using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("Shop UI")]
    [SerializeField] private GameObject _shopPanel;
    [SerializeField] private Button _closeButton;
    [SerializeField] private TextMeshProUGUI _playerGoldText;
    [SerializeField] private TextMeshProUGUI _playerCashText;
    
    [Header("Shop Tabs")]
    [SerializeField] private Button[] _tabButtons;
    [SerializeField] private GameObject[] _tabPanels;
    [SerializeField] private Color _activeTabColor = Color.white;
    [SerializeField] private Color _inactiveTabColor = Color.gray;
    
    [Header("Item Display")]
    [SerializeField] private Transform _itemContainer;
    [SerializeField] private GameObject _shopItemPrefab;
    [SerializeField] private ScrollRect _itemScrollRect;
    
    [Header("Purchase Confirmation")]
    [SerializeField] private GameObject _purchaseConfirmPanel;
    [SerializeField] private TextMeshProUGUI _purchaseItemNameText;
    [SerializeField] private TextMeshProUGUI _purchasePriceText;
    [SerializeField] private Image _purchaseItemIcon;
    [SerializeField] private Button _confirmPurchaseButton;
    [SerializeField] private Button _cancelPurchaseButton;
    
    [Header("Gacha System")]
    [SerializeField] private GameObject _gachaPanel;
    [SerializeField] private Button _singleGachaButton;
    [SerializeField] private Button _tenGachaButton;
    [SerializeField] private GameObject _gachaResultPanel;
    [SerializeField] private Transform _gachaResultContainer;
    [SerializeField] private GameObject _gachaResultItemPrefab;
    [SerializeField] private GameObject _gachaEffectPrefab;
    
    [Header("Limited Time Offers")]
    [SerializeField] private GameObject _limitedOfferPanel;
    [SerializeField] private TextMeshProUGUI _limitedOfferTimerText;
    [SerializeField] private GameObject _limitedOfferItemPrefab;
    
    [Header("Shop Items Data")]
    [SerializeField] private ShopItemData[] _weaponItems;
    [SerializeField] private ShopItemData[] _skinItems;
    [SerializeField] private ShopItemData[] _consumableItems;
    [SerializeField] private ShopItemData[] _bundleItems;
    [SerializeField] private ShopItemData[] _limitedItems;
    
    private ShopTab _currentTab = ShopTab.Weapons;
    private ShopItemData _selectedItem;
    private List<GameObject> _currentItemDisplays = new List<GameObject>();
    private Coroutine _limitedOfferCoroutine;
    
    public enum ShopTab
    {
        Weapons,
        Skins,
        Consumables,
        Bundles,
        Gacha,
        Limited
    }
    
    [System.Serializable]
    public class ShopItemData
    {
        public string itemName;
        public string description;
        public Sprite icon;
        public int goldPrice;
        public int cashPrice;
        public bool isLimited;
        public float limitedTimeRemaining;
        public int stockLimit;
        public ItemType itemType;
        public int itemValue; // 아이템 효과 값 (무기 데미지, 스킨 ID 등)
        public float discount; // 할인율 0-1
    }
    
    public enum ItemType
    {
        Weapon,
        Skin,
        Consumable,
        Bundle,
        GachaTicket
    }
    
    private void Awake()
    {
        // 버튼 이벤트 연결
        if (_closeButton) _closeButton.onClick.AddListener(CloseShop);
        if (_confirmPurchaseButton) _confirmPurchaseButton.onClick.AddListener(ConfirmPurchase);
        if (_cancelPurchaseButton) _cancelPurchaseButton.onClick.AddListener(CancelPurchase);
        if (_singleGachaButton) _singleGachaButton.onClick.AddListener(() => PerformGacha(1));
        if (_tenGachaButton) _tenGachaButton.onClick.AddListener(() => PerformGacha(10));
        
        // 탭 버튼 이벤트
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            int tabIndex = i;
            _tabButtons[i].onClick.AddListener(() => SwitchTab((ShopTab)tabIndex));
        }
    }
    
    private void OnEnable()
    {
        UpdateCurrencyDisplay();
        SwitchTab(ShopTab.Weapons);
        
        // 한정 상품 타이머 시작
        if (_limitedOfferCoroutine != null)
        {
            StopCoroutine(_limitedOfferCoroutine);
        }
        _limitedOfferCoroutine = StartCoroutine(UpdateLimitedOfferTimer());
    }
    
    private void OnDisable()
    {
        if (_limitedOfferCoroutine != null)
        {
            StopCoroutine(_limitedOfferCoroutine);
        }
    }
    
    /// <summary>
    /// 재화 표시 업데이트
    /// </summary>
    private void UpdateCurrencyDisplay()
    {
        if (_playerGoldText) _playerGoldText.text = UpgradeManager.Gold.ToString("N0");
        if (_playerCashText) _playerCashText.text = PlayerPrefs.GetInt("PlayerCash", 0).ToString("N0");
    }
    
    /// <summary>
    /// 탭 전환
    /// </summary>
    private void SwitchTab(ShopTab tab)
    {
        _currentTab = tab;
        
        // 모든 탭 패널 숨기기
        foreach (var panel in _tabPanels)
        {
            if (panel) panel.SetActive(false);
        }
        
        // 선택된 탭 패널 표시
        if ((int)tab < _tabPanels.Length && _tabPanels[(int)tab])
        {
            _tabPanels[(int)tab].SetActive(true);
        }
        
        // 탭 버튼 색상 업데이트
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            Image buttonImage = _tabButtons[i].GetComponent<Image>();
            if (buttonImage)
            {
                buttonImage.color = (i == (int)tab) ? _activeTabColor : _inactiveTabColor;
            }
        }
        
        // 아이템 표시
        switch (tab)
        {
            case ShopTab.Weapons:
                DisplayItems(_weaponItems);
                break;
            case ShopTab.Skins:
                DisplayItems(_skinItems);
                break;
            case ShopTab.Consumables:
                DisplayItems(_consumableItems);
                break;
            case ShopTab.Bundles:
                DisplayItems(_bundleItems);
                break;
            case ShopTab.Gacha:
                // 가챠 탭은 별도 처리
                break;
            case ShopTab.Limited:
                DisplayItems(_limitedItems);
                break;
        }
    }
    
    /// <summary>
    /// 아이템 표시
    /// </summary>
    private void DisplayItems(ShopItemData[] items)
    {
        // 기존 아이템 제거
        foreach (var item in _currentItemDisplays)
        {
            Destroy(item);
        }
        _currentItemDisplays.Clear();
        
        if (_itemContainer == null || _shopItemPrefab == null) return;
        
        // 새 아이템 생성
        foreach (var itemData in items)
        {
            GameObject itemObj = Instantiate(_shopItemPrefab, _itemContainer);
            _currentItemDisplays.Add(itemObj);
            
            // UI 컴포넌트 설정
            SetupShopItemUI(itemObj, itemData);
        }
        
        // 스크롤 위치 초기화
        if (_itemScrollRect)
        {
            _itemScrollRect.verticalNormalizedPosition = 1f;
        }
    }
    
    /// <summary>
    /// 상점 아이템 UI 설정
    /// </summary>
    private void SetupShopItemUI(GameObject itemObj, ShopItemData itemData)
    {
        // 아이템 이름
        TextMeshProUGUI nameText = itemObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        if (nameText) nameText.text = itemData.itemName;
        
        // 아이템 설명
        TextMeshProUGUI descText = itemObj.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
        if (descText) descText.text = itemData.description;
        
        // 아이템 아이콘
        Image iconImage = itemObj.transform.Find("IconImage")?.GetComponent<Image>();
        if (iconImage && itemData.icon) iconImage.sprite = itemData.icon;
        
        // 가격 표시
        TextMeshProUGUI priceText = itemObj.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();
        if (priceText)
        {
            if (itemData.goldPrice > 0)
            {
                priceText.text = $"<color=yellow>{itemData.goldPrice} 골드</color>";
            }
            else if (itemData.cashPrice > 0)
            {
                priceText.text = $"<color=cyan>{itemData.cashPrice} 캐시</color>";
            }
        }
        
        // 할인 표시
        if (itemData.discount > 0)
        {
            GameObject discountObj = itemObj.transform.Find("DiscountBadge")?.gameObject;
            if (discountObj)
            {
                discountObj.SetActive(true);
                TextMeshProUGUI discountText = discountObj.GetComponentInChildren<TextMeshProUGUI>();
                if (discountText)
                {
                    discountText.text = $"-{(int)(itemData.discount * 100)}%";
                }
            }
            
            // 원래 가격 표시
            TextMeshProUGUI originalPriceText = itemObj.transform.Find("OriginalPriceText")?.GetComponent<TextMeshProUGUI>();
            if (originalPriceText && itemData.goldPrice > 0)
            {
                int originalPrice = Mathf.RoundToInt(itemData.goldPrice / (1 - itemData.discount));
                originalPriceText.text = $"<s>{originalPrice}</s>";
            }
        }
        
        // 한정 상품 표시
        if (itemData.isLimited)
        {
            GameObject limitedBadge = itemObj.transform.Find("LimitedBadge")?.gameObject;
            if (limitedBadge) limitedBadge.SetActive(true);
            
            TextMeshProUGUI stockText = itemObj.transform.Find("StockText")?.GetComponent<TextMeshProUGUI>();
            if (stockText) stockText.text = $"남은 수량: {itemData.stockLimit}";
        }
        
        // 구매 버튼
        Button buyButton = itemObj.transform.Find("BuyButton")?.GetComponent<Button>();
        if (buyButton)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => OnItemClicked(itemData));
            
            // 구매 가능 여부 체크
            bool canAfford = (itemData.goldPrice > 0 && UpgradeManager.Gold >= itemData.goldPrice) ||
                           (itemData.cashPrice > 0 && PlayerPrefs.GetInt("PlayerCash", 0) >= itemData.cashPrice);
            
            buyButton.interactable = canAfford && (!itemData.isLimited || itemData.stockLimit > 0);
        }
    }
    
    /// <summary>
    /// 아이템 클릭
    /// </summary>
    private void OnItemClicked(ShopItemData item)
    {
        _selectedItem = item;
        ShowPurchaseConfirmation();
    }
    
    /// <summary>
    /// 구매 확인 창 표시
    /// </summary>
    private void ShowPurchaseConfirmation()
    {
        if (_purchaseConfirmPanel == null || _selectedItem == null) return;
        
        _purchaseConfirmPanel.SetActive(true);
        
        if (_purchaseItemNameText) _purchaseItemNameText.text = _selectedItem.itemName;
        if (_purchaseItemIcon && _selectedItem.icon) _purchaseItemIcon.sprite = _selectedItem.icon;
        
        if (_purchasePriceText)
        {
            if (_selectedItem.goldPrice > 0)
            {
                _purchasePriceText.text = $"{_selectedItem.goldPrice} 골드";
            }
            else if (_selectedItem.cashPrice > 0)
            {
                _purchasePriceText.text = $"{_selectedItem.cashPrice} 캐시";
            }
        }
    }
    
    /// <summary>
    /// 구매 확인
    /// </summary>
    private void ConfirmPurchase()
    {
        if (_selectedItem == null) return;
        
        bool purchaseSuccess = false;
        
        // 골드로 구매
        if (_selectedItem.goldPrice > 0)
        {
            if (UpgradeManager.Gold >= _selectedItem.goldPrice)
            {
                UpgradeManager.Gold -= _selectedItem.goldPrice;
                UpgradeManager.SaveData();
                purchaseSuccess = true;
            }
        }
        // 캐시로 구매
        else if (_selectedItem.cashPrice > 0)
        {
            int currentCash = PlayerPrefs.GetInt("PlayerCash", 0);
            if (currentCash >= _selectedItem.cashPrice)
            {
                PlayerPrefs.SetInt("PlayerCash", currentCash - _selectedItem.cashPrice);
                PlayerPrefs.Save();
                purchaseSuccess = true;
            }
        }
        
        if (purchaseSuccess)
        {
            // 아이템 지급
            GrantItem(_selectedItem);
            
            // 재고 감소
            if (_selectedItem.isLimited && _selectedItem.stockLimit > 0)
            {
                _selectedItem.stockLimit--;
            }
            
            // UI 업데이트
            UpdateCurrencyDisplay();
            SwitchTab(_currentTab); // 아이템 목록 새로고침
            
            // 구매 성공 효과
            ShowPurchaseSuccessEffect();
        }
        else
        {
            // 구매 실패 메시지
            ShowPurchaseFailedMessage();
        }
        
        _purchaseConfirmPanel.SetActive(false);
    }
    
    /// <summary>
    /// 구매 취소
    /// </summary>
    private void CancelPurchase()
    {
        _purchaseConfirmPanel.SetActive(false);
        _selectedItem = null;
    }
    
    /// <summary>
    /// 아이템 지급
    /// </summary>
    private void GrantItem(ShopItemData item)
    {
        switch (item.itemType)
        {
            case ItemType.Weapon:
                // 무기 잠금 해제
                PlayerPrefs.SetInt($"Weapon_{item.itemValue}_Unlocked", 1);
                break;
                
            case ItemType.Skin:
                // 스킨 잠금 해제
                PlayerPrefs.SetInt($"Skin_{item.itemValue}_Unlocked", 1);
                break;
                
            case ItemType.Consumable:
                // 소모품 지급
                int currentAmount = PlayerPrefs.GetInt($"Item_{item.itemValue}", 0);
                PlayerPrefs.SetInt($"Item_{item.itemValue}", currentAmount + 1);
                break;
                
            case ItemType.Bundle:
                // 번들 내용물 지급 (별도 처리 필요)
                GrantBundleContents(item.itemValue);
                break;
                
            case ItemType.GachaTicket:
                // 가챠 티켓 지급
                int tickets = PlayerPrefs.GetInt("GachaTickets", 0);
                PlayerPrefs.SetInt("GachaTickets", tickets + item.itemValue);
                break;
        }
        
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 번들 내용물 지급
    /// </summary>
    private void GrantBundleContents(int bundleId)
    {
        // 번들 ID에 따른 내용물 지급
        switch (bundleId)
        {
            case 1: // 스타터 팩
                UpgradeManager.Gold += 1000;
                PlayerPrefs.SetInt("GachaTickets", PlayerPrefs.GetInt("GachaTickets", 0) + 5);
                break;
            case 2: // 프리미엄 팩
                UpgradeManager.Gold += 5000;
                PlayerPrefs.SetInt("GachaTickets", PlayerPrefs.GetInt("GachaTickets", 0) + 20);
                PlayerPrefs.SetInt("Skin_Premium_Unlocked", 1);
                break;
        }
        
        UpgradeManager.SaveData();
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 가챠 수행
    /// </summary>
    private void PerformGacha(int count)
    {
        int tickets = PlayerPrefs.GetInt("GachaTickets", 0);
        
        if (tickets < count)
        {
            ShowInsufficientTicketsMessage();
            return;
        }
        
        // 티켓 차감
        PlayerPrefs.SetInt("GachaTickets", tickets - count);
        PlayerPrefs.Save();
        
        // 가챠 결과 생성
        List<GachaResult> results = new List<GachaResult>();
        for (int i = 0; i < count; i++)
        {
            results.Add(GenerateGachaResult());
        }
        
        // 결과 표시
        ShowGachaResults(results);
    }
    
    /// <summary>
    /// 가챠 결과 생성
    /// </summary>
    private GachaResult GenerateGachaResult()
    {
        float rand = Random.value;
        
        // 가챠 확률
        // SSR: 2%, SR: 8%, R: 30%, N: 60%
        if (rand < 0.02f)
        {
            return new GachaResult { rarity = "SSR", itemName = "전설 스킨", itemType = ItemType.Skin };
        }
        else if (rand < 0.1f)
        {
            return new GachaResult { rarity = "SR", itemName = "희귀 무기", itemType = ItemType.Weapon };
        }
        else if (rand < 0.4f)
        {
            return new GachaResult { rarity = "R", itemName = "일반 스킨", itemType = ItemType.Skin };
        }
        else
        {
            return new GachaResult { rarity = "N", itemName = "골드 100", itemType = ItemType.Consumable };
        }
    }
    
    [System.Serializable]
    private class GachaResult
    {
        public string rarity;
        public string itemName;
        public ItemType itemType;
    }
    
    /// <summary>
    /// 가챠 결과 표시
    /// </summary>
    private void ShowGachaResults(List<GachaResult> results)
    {
        if (_gachaResultPanel == null) return;
        
        _gachaResultPanel.SetActive(true);
        
        // 기존 결과 제거
        foreach (Transform child in _gachaResultContainer)
        {
            Destroy(child.gameObject);
        }
        
        // 결과 표시
        foreach (var result in results)
        {
            GameObject resultObj = Instantiate(_gachaResultItemPrefab, _gachaResultContainer);
            
            // 희귀도에 따른 색상
            Color rarityColor = Color.white;
            switch (result.rarity)
            {
                case "SSR": rarityColor = Color.yellow; break;
                case "SR": rarityColor = Color.magenta; break;
                case "R": rarityColor = Color.cyan; break;
            }
            
            TextMeshProUGUI rarityText = resultObj.transform.Find("RarityText")?.GetComponent<TextMeshProUGUI>();
            if (rarityText)
            {
                rarityText.text = result.rarity;
                rarityText.color = rarityColor;
            }
            
            TextMeshProUGUI itemText = resultObj.transform.Find("ItemText")?.GetComponent<TextMeshProUGUI>();
            if (itemText) itemText.text = result.itemName;
            
            // 효과
            if (_gachaEffectPrefab && result.rarity == "SSR")
            {
                Instantiate(_gachaEffectPrefab, resultObj.transform);
            }
        }
    }
    
    /// <summary>
    /// 한정 상품 타이머 업데이트
    /// </summary>
    private IEnumerator UpdateLimitedOfferTimer()
    {
        while (true)
        {
            if (_limitedOfferTimerText && _limitedItems != null)
            {
                // 가장 빠르게 끝나는 한정 상품의 시간 표시
                float minTime = float.MaxValue;
                foreach (var item in _limitedItems)
                {
                    if (item.isLimited && item.limitedTimeRemaining < minTime)
                    {
                        minTime = item.limitedTimeRemaining;
                    }
                }
                
                if (minTime < float.MaxValue)
                {
                    int hours = Mathf.FloorToInt(minTime / 3600);
                    int minutes = Mathf.FloorToInt((minTime % 3600) / 60);
                    int seconds = Mathf.FloorToInt(minTime % 60);
                    
                    _limitedOfferTimerText.text = $"한정 상품 종료까지: {hours:00}:{minutes:00}:{seconds:00}";
                    
                    // 시간 감소
                    foreach (var item in _limitedItems)
                    {
                        if (item.isLimited)
                        {
                            item.limitedTimeRemaining -= 1f;
                        }
                    }
                }
            }
            
            yield return new WaitForSeconds(1f);
        }
    }
    
    /// <summary>
    /// 구매 성공 효과
    /// </summary>
    private void ShowPurchaseSuccessEffect()
    {
        // 구매 성공 사운드 및 이펙트
        Debug.Log("구매 성공!");
    }
    
    /// <summary>
    /// 구매 실패 메시지
    /// </summary>
    private void ShowPurchaseFailedMessage()
    {
        Debug.Log("재화가 부족합니다!");
    }
    
    /// <summary>
    /// 티켓 부족 메시지
    /// </summary>
    private void ShowInsufficientTicketsMessage()
    {
        Debug.Log("가챠 티켓이 부족합니다!");
    }
    
    /// <summary>
    /// 상점 닫기
    /// </summary>
    private void CloseShop()
    {
        _shopPanel.SetActive(false);
    }
}