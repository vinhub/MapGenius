using UnityEngine;
using System.Collections;
using System.Linq;

public class ActivateOnCollision : MonoBehaviour {

	public Rigidbody rBody;
	public SphereCollider triggerCol;
	public float triggerRadius = 1f;
	public AudioClip[] hitClips;
	private AudioSource aSource;
	public GameObject holder;
	// Use this for initialization
	void Start () {
		//Grab Audio Clips
		hitClips = Resources.LoadAll("HitClips", typeof(AudioClip)).Cast<AudioClip>().ToArray();
		if(hitClips.Length > 0){
			aSource = gameObject.AddComponent<AudioSource> ();
		}
		if(rBody == null){
			rBody = gameObject.GetComponent<Rigidbody> ();
		}
		triggerCol = gameObject.AddComponent<SphereCollider> ();
		triggerCol.isTrigger = true;
		triggerCol.radius = triggerRadius;
	}

	void OnTriggerEnter(Collider other) {
		if(other.tag == "Player"){
			rBody.isKinematic = false;
			Destroy (triggerCol);
		}
	}

	private bool active = false;

	void OnCollisionEnter(Collision other){
		if(other.collider.tag == "Player"){
			if(hitClips.Length > 0){
				//Play Sound
				int randomHit = Mathf.RoundToInt(Random.Range(0,hitClips.Length));
				aSource.PlayOneShot(hitClips[randomHit]);
			}
			if(!active){
				active = true;
				StartCoroutine("SelfDestruct");
			}
		}
	}

	IEnumerator SelfDestruct(){
		yield return new WaitForSeconds(5);
		if(holder){
			DestroyImmediate (holder);
		} else {
			DestroyImmediate(transform.gameObject);
		}
	}
}
