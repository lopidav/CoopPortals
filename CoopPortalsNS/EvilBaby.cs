using UnityEngine;
namespace CoopPortalsNS;
public class EvilBaby : Eviler
{
    public override bool CanMove => false;
    public override void Awake()
    {
		this.BaseCombatStats = new CombatStats();
		this.BaseCombatStats.SpecialHits = new List<SpecialHit>();
		this.HealthPoints = 1;
        base.Awake();
    }
    
	public override void UpdateCard()
	{
		AttackTimer = 0f;
		base.UpdateCard();
	}

    
    
	public override void Move()
	{
    }
}