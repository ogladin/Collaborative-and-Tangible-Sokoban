#define DEBUG_LOGGER

using UnityEngine;
using UnityEngine.Serialization;

public class Crate : MonoBehaviour
{
	private int BlueCrateLayerId;
	private int GreenCrateLayerId;
	private int BluePlayerLayerId;
	private int GreenPlayerLayerId;
	public LayerMask DotMask;


	public Material WrongPlaceMaterial;
	public Material GoodPlaceMaterial;


	[FormerlySerializedAs("WallMask")] public LayerMask TargetMask;
	public LayerMask FromMask;
	public int NetworkId;

	// Start is called before the first frame update
	void Start()
	{
		BluePlayerLayerId = LayerMask.NameToLayer("BluePlayer");
		GreenPlayerLayerId = LayerMask.NameToLayer("GreenPlayer");

		BlueCrateLayerId = LayerMask.NameToLayer("BlueCrate");
		GreenCrateLayerId = LayerMask.NameToLayer("GreenCrate");
	}

	// Update is called once per frame
	void Update()
	{
	}

	void OnCollisionEnter(Collision collision)
	{
		Vector3 normal = collision.contacts[0].normal;

		bool isValidPosition = true;
		if (collision.collider.gameObject.layer == BluePlayerLayerId)
		{
			isValidPosition = LevelManager.Instance.CheckGridPosition(1);
		}
		else if (collision.collider.gameObject.layer == GreenPlayerLayerId)
		{
			isValidPosition = LevelManager.Instance.CheckGridPosition(0);
		}

		if (!isValidPosition) return;

		if ((collision.collider.gameObject.layer == BluePlayerLayerId && gameObject.layer == BlueCrateLayerId) ||
		    (collision.collider.gameObject.layer == GreenPlayerLayerId && gameObject.layer == GreenCrateLayerId))
		{
			if (normal == transform.right)
			{
				TryToMove(normal);
				//Debug.Log("RIGHT");
			}

			else if (normal == -(transform.right))
			{
				TryToMove(normal);
				//Debug.Log("LEFT");
			}

			else if (normal == transform.up)
			{
				//Debug.Log("UP");
				TryToMove(-transform.forward);
			}

			else if (normal == -(transform.up))
			{
				//Debug.Log("DOWN");
				TryToMove(transform.forward);
			}
		}
	}

	public void SetGoodPlaceMaterial()
	{
		GetComponent<Renderer>().material = GoodPlaceMaterial;
	}

	public void SetWrongPlaceMaterial()
	{
		GetComponent<Renderer>().material = WrongPlaceMaterial;
	}


	void TryToMove(Vector3 direction)
	{
		if (!Physics.CheckSphere(transform.TransformPoint(direction), transform.lossyScale.x / 2.5f, TargetMask)) //radius is a bit less than half the size of a square
		{
			//Push to a valid position
			if (!Physics.CheckSphere(transform.TransformPoint(-direction), transform.lossyScale.x / 2.5f, FromMask))
			{
				//Push from a valid position
				transform.Translate(direction * transform.lossyScale.x);
				LevelManager.Instance.updateCratePosition = true;
				int id;
				if (GreenCrateLayerId == gameObject.layer)
					id = 0;
				else
					id = 1;
				LevelManager.Instance.IncreaserMoveCounter(id);
				FileLogger.Logger.LogString("Crate has been pushed");
			}
		}
	}

	public void CheckPosition()
	{
		if (Physics.CheckSphere(transform.TransformPoint(Vector3.zero), transform.lossyScale.x/3, DotMask))
		{
			SetGoodPlaceMaterial();
		}
		else
		{
			SetWrongPlaceMaterial();
		}
	}

	//   private void OnDrawGizmos()
	//   {
	//    if (!Physics.CheckSphere(transform.TransformPoint(transform.forward), transform.lossyScale.x  / 3, ObstacleMask))
	//     Gizmos.color = Color.green;
	// else
	//     Gizmos.color = Color.red;
	//
	//    Gizmos.DrawWireSphere(transform.TransformPoint(transform.forward), transform.lossyScale.x  / 3);
	//
	//   }
}
