using UnityEngine;
namespace CoopPortalsNS;
public class Eviler : Enemy
{
    public override void Die()
    {
        this.GetAllEquipables().ForEach(e => {this.MyGameCard.Unequip(e);e.MyGameCard.SendIt();});
        base.Die();
    }
    public override void UpdateCardText()
    {
        if (string.IsNullOrEmpty(descriptionOverride))
        {
            descriptionOverride = SokLoc.Translate(DescriptionTerm);
            descriptionOverride += "\n\n";
            descriptionOverride = descriptionOverride + "<i>" + GetCombatableDescription() + "</i>";
            if (AdvancedSettingsScreen.AdvancedCombatStatsEnabled || GameCanvas.instance.CurrentScreen is CardopediaScreen)
            {
                descriptionOverride = descriptionOverride + "\n\n<i>" + GetCombatableDescriptionAdvanced() + "</i>";
            }
        }
        if (string.IsNullOrEmpty(CustomName))
        {
            nameOverride = "Evil "+SokLoc.Translate(this.NameTerm);
        }
        else
        {
            nameOverride = CustomName;
        }
    }
}