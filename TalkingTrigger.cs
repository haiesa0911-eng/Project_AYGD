using UnityEngine;

public class TalkingTrigger : MonoBehaviour
{
    Animator anim;

    void Awake() => anim = GetComponent<Animator>();

    // Contoh pemicu manual (tombol Space)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            anim.SetTrigger("TalkTrigger");
    }

    // Panggil fungsi ini dari event lain (mis. klik, dialog, dsb.)
    public void PlayTalkingOnce()
    {
        anim.SetTrigger("TalkTrigger");
    }
}
