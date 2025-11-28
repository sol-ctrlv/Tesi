using UnityEngine;

public class InitCharacter : MonoBehaviour
{
    public CharacterSO CharacterClass;
    public GameObject CharacterAvatar;
    public CharacterStatHolder CharacterStats;
    public TargetDetection TargetDetection;
    public AttackSpawner AttackSpawner;
    public Player player;
    [SerializeField] private Transform gunHolder;

    private void Awake()
    {
        if (CharacterSelecter.selectedCharacter != null)
            CharacterClass = CharacterSelecter.selectedCharacter;
    }

    void Start()
    {
        CharacterAvatar.GetComponent<Renderer>().material.SetColor("_Color", CharacterClass.Color);
        CharacterAvatar.GetComponent<SpriteRenderer>().sprite = CharacterClass.Sprite;
        CharacterStats = GetComponent<CharacterStatHolder>();
        CharacterStats.InitStat(CharacterClass);
        TargetDetection = GetComponentInChildren<TargetDetection>();
        TargetDetection.Init(CharacterStats.atkRadius);

        BasicAttackBehaviour defaultAttack = AttackSpawner.Spawn(CharacterClass.DefaultAttack, gunHolder);
        defaultAttack.SetNewDamageMultiplier(CharacterStats.StatMulti(defaultAttack.ScalingStat));
        player.Init(CharacterStats.StatMulti(Stats.cos) * 100);
    }
}
