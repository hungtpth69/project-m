using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine.Events;

public class Life : MonoBehaviour
{
    [SerializeField] private int maxHealth; //Number of hits taken to die
    [SerializeField] protected int health;    //Current number of hits taken to die
    private ILife lifeBehavior;

    public string triggerTag { get; private set; }


    [Header("Sound effects (indexes from sound manager)")]
    //0: Hurt sound
    //1: Normal sound               // These are the indexes of the below elements (aranged in order)
    //2: Death sound
    //3: ...
    [SerializeField] private int hurtSound;
    [SerializeField] private int deathSound;


    [Header("Particle effects")]
    private int directionalHurtParticles = 2;
    [SerializeField] private bool hasBloodSplash;
    [HideInInspector][SerializeField] private Color bloodColor;
    [SerializeField] private int[] hurtParticles;
    [SerializeField] private int deathParticles;

    [Header("AttackFX")]
    [SerializeField] private GameObject attackFx;

    [Header("Sprite (Tint when damaged)")]
    private SpriteRenderer[] spriteRenderers;
    private MeshRenderer spineMeshRenderer;
    private SkeletonMecanim spineSkeletonMecanim;

    private Color originalSpineColor;
    private Color tintSpineColor;
    private Color originalSpriteColor;
    [SerializeField] private Color tintSpriteColor;

    [Header("Corpse and drops (appear when object dies)")]
    [SerializeField] private GameObject[] corpse;
    [SerializeField] private Drop[] drops;

    public UnityEvent OnDamaged;


    protected virtual void Start()
    {
        health = maxHealth;
        //Debug.Log("Health: " + health);
        if (GetComponent<ILife>() != null)
        {
            lifeBehavior = GetComponent<ILife>();

            //Trigger tag
            triggerTag = lifeBehavior.triggerDamageTag;
            Debug.LogError(this.gameObject + " has trigger tag:" + triggerTag);
        }
        if (triggerTag == null || triggerTag == "")
        {
            Debug.Log("TriggerTag is null in: " + this.gameObject.name);
        }

        //Sprite renderer & Mesh renderer
        //*Note: Should only find Sprite renderer if there is no mesh renderer!
        spineMeshRenderer = GetComponentInChildren<MeshRenderer>();
        if(spineMeshRenderer == null) spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        if (GetComponentInChildren<SkeletonMecanim>() != null) spineSkeletonMecanim = GetComponentInChildren<SkeletonMecanim>();

        //Tint color
        originalSpineColor = new Color(1, 1, 1, 1);
        tintSpineColor = new Color(1, 0, 0, 1);

        originalSpriteColor = new Color(1, 1, 1, 1);
        //tintSpriteColor = new Color(1, 1, 1, 1);

        //Attack FX
        if (attackFx == null)
        {
            Debug.LogError("Please insert ATTACK FX to the object: " + this.gameObject);
        }
    }

    public int GetHealth()
    {
        return health;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public bool HasBloodSplash()
    {
        return hasBloodSplash;
    }

    public void doBloodSplash(Collider2D damager)
    {
        Vector2 lookDir = transform.position - damager.transform.parent.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        angle = 90 - angle;
        Quaternion rotation = Quaternion.Euler(angle - 90f, 90f, -90f);

        ParticleSystem blood = ParticlesManager.instance.SpawnDirectionalParticle(directionalHurtParticles, transform.position, rotation).GetComponent<ParticleSystem>();
        ParticlesManager.instance.setParticleColor(blood, bloodColor);
    }

    public virtual void TakeDamage(int damageTaken)
    {
        //Hurt sound
        PlayHurtSound();

        //Hurt particles
        SpawnHurtParticles();

        //Spawn attack FX
        SpawnAttackFX();

        health = health - damageTaken;

        if (health <= 0)
        {
            Die();
        }

        OnDamaged.Invoke();
    }

    public virtual void Heal(int amount)
    {
        health = health + amount;
    }

    public void PlayHurtSound()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayRandomPitchSFX(hurtSound);    //Play the hurt sound of the entity (because 0 is the index of this entity hurt sound element in the Audio Manager)
        }
    }
    public void SpawnHurtParticles()
    {
        if (ParticlesManager.instance != null)
        {
            foreach (int hurtParticle in hurtParticles)
            {
                ParticlesManager.instance.SpawnParticle(hurtParticle, transform.position);
            }
        }
    }
    public void SpawnAttackFX()
    {
        if (attackFx != null)
        {
            Instantiate(attackFx, transform.position, Quaternion.Euler(0, Random.Range(0, 4) * 90, 0));
        }
    }

    public virtual void Die()
    {
        if (ParticlesManager.instance != null)
        {
            ParticlesManager.instance.SpawnParticle(deathParticles, transform.position);
        }
        SpawnDrop();
        Destroy(gameObject);
    }
    public void SpawnDrop()
    {
        foreach(Drop drop in drops)
        {
            for(int i = 0; i < drop.getAmount(); i++)
            {
                GameObject theDrop = Instantiate(drop.getDropObject(), transform.position, Quaternion.identity);

                float dropForce = 3f;
                Vector2 dropDir = new Vector2(Random.Range(-1f, 1f), 1f);
                theDrop.GetComponent<Rigidbody2D>().AddForce(dropDir * dropForce, ForceMode2D.Impulse);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(triggerTag + " : " + this.gameObject.name);
        if (collision.CompareTag(triggerTag))
        {
            GameObject thePlayer = null;
            if (triggerTag == "Attack")     //If this is attacked by player
            {
                thePlayer = collision.GetComponentInParent<PlayerSingleton>().gameObject;
            }

            //Take damage
            int damage = collision.gameObject.GetComponent<Damage>().GetDamage();
            TakeDamage(damage);

            //Camshake
            GameObject theCamshake = null;
            if (thePlayer != null)
            {
                theCamshake = thePlayer.transform.Find("Camshake").gameObject;
                theCamshake.GetComponent<Camshake>().PlayCamShake();
            }

            //Tint when damaged
            TintColor();

            //Spawn directional particle (collision is direction root)  *Only non-player objects have this particles
            if(hasBloodSplash == true && ParticlesManager.instance != null)
            {
                doBloodSplash(collision);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        //Reset tint color
        ResetTint();
    }

    public void TintColor()
    {
        if (spriteRenderers != null)
        {
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                //renderer.material.SetColor("_Color", tintSpriteColor);
                renderer.color = tintSpriteColor;
            }
        }
        else if (spineSkeletonMecanim != null)
        {
            /*MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Black", tintSpineColor);
            spineMeshRenderer.SetPropertyBlock(mpb);*/

            spineSkeletonMecanim.Skeleton.SetColor(tintSpineColor);
        }
    }

    public void ResetTint()
    {
        if (spriteRenderers != null)
        {
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                //renderer.material.SetColor("_Color", originalSpriteColor);
                renderer.color = originalSpriteColor;
            }
        }
        else if (spineMeshRenderer != null)
        {
            /*MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Black", originalSpineColor);
            spineMeshRenderer.SetPropertyBlock(mpb);*/

            spineSkeletonMecanim.Skeleton.SetColor(originalSpineColor);
        }
    }

    void Update()
    {
        /*if (health <= 0)
        {
            Die();
        }*/
    }

    //Show or hide variables on inspector
    #if UNITY_EDITOR
    [CustomEditor(typeof(Life))]
    public class LifeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            //Call normal GUI (displaying all other variables that exist)
            base.OnInspectorGUI();

            //Reference the script
            Life script = (Life)target;

            if (script.hasBloodSplash == true)           //If the variable 'hasBloodSplash' is set to TRUE
            {
                EditorGUILayout.BeginHorizontal();      // Ensure the label and the value are on the same line

                script.bloodColor = EditorGUILayout.ColorField(script.bloodColor);      //Enable a color field for the 'bloodColor' field of the script

                EditorGUILayout.EndHorizontal();
            }
        }
    }
    #endif
}
