using UnityEngine;

public enum TurretType { Bullet, Rocket }

public class TurretView : MonoBehaviour
{
    public TurretType turretType = TurretType.Bullet;
    public Animator animator;
    public ParticleSystem particles;

    public void PlayShoot()
    {
        if (animator != null) {
            if (turretType == TurretType.Bullet) {
                animator.SetTrigger("Shoot");
            } else {
                animator.SetTrigger("RocketShoot");
            }
        }
            
        if (particles != null)
            particles.Play();
    }

    public void PlayParticlesOnly() {
        if (particles != null) {
            particles.Play();
        }
    }
}