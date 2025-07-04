using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonSoundTrigger : MonoBehaviour
{
    // Simple methods you can call from button OnClick events
    public void PlayDeadSound()
    {
        SoundPracticePlayer.Instance.PlaySound(SoundEnum.DeadSound);
    }

    public void PlayClickSound()
    {
        SoundPracticePlayer.Instance.PlaySound(SoundEnum.ClickSound);
    }

    public void PlayAttackSound()
    {
        SoundPracticePlayer.Instance.PlaySound(SoundEnum.AttackSound);
    }

    // Or make one generic method that takes a string parameter
    public void PlaySoundByName(string soundName)
    {
        if (System.Enum.TryParse(soundName, out SoundEnum soundType))
        {
            SoundPracticePlayer.Instance.PlaySound(soundType);
        }
    }
}
