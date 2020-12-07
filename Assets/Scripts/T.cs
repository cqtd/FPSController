// using UnityEngine;
//
// public class Bullet : MonoBehaviour
// {
//     public struct BulletContext
//     {
//         public float damage;
//         public Shooter shooter;
//     }
//
//     private BulletContext m_context;
//
//     public void Initialize(BulletContext context)
//     {
//         this.m_context = context;
//     }
//
//     public BulletContext GetContext()
//     {
//         return m_context;
//     }
// }
//
// public class Shooter : MonoBehaviour
// {
//     public float health = 100;
//     public float power = 20;
//     
//     void Fire()
//     {
//         var bullet = Resources.Load<Bullet>("Prefab/Bullet");
//         bullet.Initialize(new Bullet.BulletContext()
//         {
//             damage = power,
//             shooter = this,
//         });
//         
//         // 총알 물리 로직 실행
//     }
//
//     void OnShotBullet(Bullet bullet)
//     {
//         var ctx = bullet.GetContext();
//         health -= ctx.damage;
//         
//         Debug.Log($"Shooter is {ctx.shooter.name}");
//     }
// }