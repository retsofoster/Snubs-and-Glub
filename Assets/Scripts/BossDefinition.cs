using UnityEngine;

[CreateAssetMenu(fileName = "NewBoss", menuName = "Game/Boss Definition")]
public class BossDefinition : ScriptableObject
{
    public string bossName;
    public int maxHealth;
    public int startingShield;
    public Sprite portrait;
}
