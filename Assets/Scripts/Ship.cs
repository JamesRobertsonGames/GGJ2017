﻿using UnityEngine;
using System.Collections;

using DG.Tweening;

public class Ship : MonoBehaviour {

    float rotationTimer;
    public LayerMask water;

    private bool active;
    private bool animate;

    public int maxMoves = 2;
    public int maxRange = 2;
    public int damage = 5;
    private int hiddenDamage = 3;

    public int health = 15;

    bool ready;
    public GameObject readyObject;
    public GameObject model;
    public GameObject healthBarChunk;

    public GameObject SinkingSmoke;
    public GameObject CannonExplode;

    public GameObject cannonball;
    public Transform cannonballSpawn;

    GameObject CrashSmoke;
    GameObject Splosion;

    public int team;

    GameObject bar1;
    GameObject bar2;
    GameObject bar3;
    GameObject bar4;

    bool healthfirsttime = true;

    // Use this for initialization
    void Start()
    {
        rotationTimer = Random.Range(0, 90);
        active = true;
        animate = true;

        ready = false;
        readyObject.SetActive(false);
	}
	
	// Update is called once per frame
	void Update()
    {
        CalculateHealthBar();
        if(transform.position.y > 8)
        {
            active = false;
            StartCoroutine(WaveReaction());
        }
        if(active)
        {
            if (animate)
            {
                rotationTimer += Time.deltaTime;

                Vector3 rot = transform.rotation.eulerAngles;
                model.transform.rotation = Quaternion.Euler(rot.x, rot.y, Mathf.Sin(rotationTimer) * 4);
            }

            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 10, water))
            {
                transform.position = hit.point;
            }
        }

        if(ready)
        {
            readyObject.transform.LookAt(Camera.main.transform);
            Vector3 rot = readyObject.transform.rotation.eulerAngles;
            readyObject.transform.rotation = Quaternion.Euler(rot.x, rot.y, Mathf.Sin(rotationTimer * 3) * 6);
            float scale = 30 + Mathf.Sin((rotationTimer + 90) * 1.5f) * 5f;
            readyObject.transform.localScale = new Vector3(scale, scale, scale);
        }
	}

    public void UseShip(bool b)
    {
        active = b;
    }

    private bool falling = false;
    private Vector3 waveRot;
    IEnumerator WaveReaction()
    {
        waveRot = transform.rotation.eulerAngles;
        if(!falling)
        {
            falling = true;

            Vector3 randomRotation;
            randomRotation.x = Random.Range(-10, 10);
            randomRotation.y = Random.Range(-10, 10);
            randomRotation.z = Random.Range(-10, 10);

            bool up = true;
            bool fallingWithStyle = true;

            WorldGenerator.GetInstance().GetTile(transform.position).occupant = null;
            while (fallingWithStyle)
            {
                transform.position += new Vector3(0, 50 * (up ? 1 : -1) * Time.deltaTime);
                transform.Rotate(randomRotation * (up ? 1 : -1) * Time.deltaTime * 20);
                
                if(!up)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 1))
                    {
                        if(hit.collider.tag == "Scenery")
                        {
                            transform.position = hit.point;
                            animate = false;
                            fallingWithStyle = false;
                            TakeDamage(8);
                            break;
                        }
                        else if(hit.collider.tag == "Water")
                        {
                            transform.position = hit.point;
                            
                            transform.rotation = Quaternion.Euler(waveRot);

                            animate = true;
                            fallingWithStyle = false;
                            TakeDamage(5);
                            break;
                        }
                    }
                }

                if (transform.position.y > 100)
                { up = false; transform.position = new Vector3(transform.position.x, 100, transform.position.z + WorldGenerator.GetInstance().TILESIZE); }

                yield return null;
            }
            WorldGenerator.GetInstance().GetTile(transform.position).occupant = gameObject;

            falling = false;
            active = true;
            yield return null;
        }
    }

    public void Restart()
    {
        ready = true;
    }

    public void Alert()
    {
        readyObject.SetActive(true);
    }

    public bool FinishedTurn()
    {
        if (temp)
            return true;

        return !ready;
    }

    public void EndTurn()
    {
        ready = false;
        readyObject.SetActive(false);
    }

    public void Select(bool b)
    {
        if(b)
        {
            WorldGenerator.GetInstance().FindReachableTiles(transform.position, maxMoves, maxRange, team);
        }
        else
        {
            WorldGenerator.GetInstance().ClearReachableTiles();
        }
    }

    public void ShowAttackRange()
    {
        WorldGenerator.GetInstance().ClearReachableTiles();
        WorldGenerator.GetInstance().FindReachableTiles(transform.position, 0, maxRange, team);
    }

    protected Vector3 startRot;
    public virtual void Attack(Ship target)
    {
        startRot = transform.rotation.eulerAngles;
        transform.DORotate(Quaternion.LookRotation(target.transform.position - transform.position, Vector3.up).eulerAngles, 1);
        StartCoroutine(FireCannon(target));
    }

    protected IEnumerator FireCannon(Ship target)
    {
        yield return new WaitForSeconds(1.2f);
        GameObject ball = Instantiate(cannonball, cannonballSpawn.position, Quaternion.identity) as GameObject;

        float d = Vector3.Distance(cannonballSpawn.position, target.transform.position) * 0.4f;
        Vector3 direction = target.transform.position - cannonballSpawn.position + Vector3.up;

        float shortTimer = 0;
        do
        {
            shortTimer += Time.deltaTime;
            ball.transform.position += direction * d * Time.deltaTime;
            ball.transform.position += new Vector3(0, (shortTimer >= 0.5f ? 2 : -2) * 5 * Time.deltaTime);
            yield return null;
        } while (shortTimer < 1);

        target.TakeDamage(GetDamage());
        transform.DORotate(startRot, 1);
        Splosion = Instantiate(CannonExplode);
        Splosion.transform.position = target.transform.position;
        Destroy(ball.gameObject);
        yield return null;
    }
    
    int GetDamage()
    {
        return Random.Range(damage, damage + hiddenDamage);
    }

    public void TakeDamage(int n)
    {
        health -= n;
        CalculateHealthBar();
        if (health <= 0)
            Kill();
    }

    public void CalculateHealthBar()
    {
        if (healthfirsttime)
        {
            bar1 = Instantiate(healthBarChunk);
            bar2 = Instantiate(healthBarChunk);
            bar3 = Instantiate(healthBarChunk);
            bar4 = Instantiate(healthBarChunk);

            bar1.transform.Rotate(90, 90, 0);
            bar2.transform.Rotate(90, 90, 0);
            bar3.transform.Rotate(90, 90, 0);
            bar4.transform.Rotate(90, 90, 0);

            healthfirsttime = false;
        }
        if (health > 0)
        {
           
            bar1.transform.position = new Vector3(model.transform.position.x - 3, model.transform.position.y + 7, model.transform.position.z);
        }
        else
        {
            if (bar1)
                Destroy(bar1);
        }
        if (health > 5)
        {
           
            bar2.transform.position = new Vector3(model.transform.position.x - 1, model.transform.position.y + 7, model.transform.position.z);
        }
        else
        {
            if (bar2)
                Destroy(bar2);
        }
        if (health > 10)
        {
            
            bar3.transform.position = new Vector3(model.transform.position.x + 1, model.transform.position.y + 7, model.transform.position.z);
        }
        else
        {
            if (bar3)
                Destroy(bar3);
        }
        if (health > 15)
        {

            bar4.transform.position = new Vector3(model.transform.position.x + 3, model.transform.position.y + 7, model.transform.position.z);
        }
        else
        {
            if (bar4)
                Destroy(bar4);
        }
    }

    [HideInInspector] public bool killFlag;
    void Kill()
    {
        killFlag = true;
        active = false;
        animate = false;
        ready = false;
        WorldGenerator.GetInstance().GetTile(transform.position).occupant = null;

        CrashSmoke = Instantiate(SinkingSmoke);

        StartCoroutine(Sink());
    }

    IEnumerator Sink()
    {
        do
        {
            transform.position -= Vector3.up * 2 * Time.deltaTime;
            CrashSmoke.transform.position = transform.position;
            yield return null;
        } while(transform.position.y > 0);
        
        yield return null;
    }

    private bool temp;
    public void Disable()
    {
        temp = true;
    }
}
