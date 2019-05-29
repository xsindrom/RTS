using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
//using Unity.Burst;
using Photon.Pun;
namespace Units.PureECS
{
    public class PhotonSyncJobSystem : JobComponentSystem
    {
        //[BurstCompile]
        struct PhotonSyncJobHandler : IJobProcessComponentData<PhotonSyncTransform, Position, Rotation>
        {
            public float serializationRate;
            public double time;
            public double serverTime;

            public void Execute([ReadOnly] ref PhotonSyncTransform transform,  ref Position position, ref Rotation rotation)
            {
                double lag = math.abs(time - transform.infoTime);
                float3 networkPosition = transform.networkPosition + transform.direction * (float)lag;
                float distance = math.distance(position.Value, transform.networkPosition);

                //Position
                float3 toVector = transform.networkPosition - position.Value;
                float mag = math.sqrt(toVector.x * toVector.x + toVector.y * toVector.y + toVector.z * toVector.z);

                position.Value = mag == 0 ? position.Value : position.Value + toVector / mag * (distance / serializationRate);
                ///Rotation
                float dot = math.dot(rotation.Value, transform.networkRotation);
                float angle = dot > 1.0f - 0.000001f ? 0.0f : math.degrees(math.acos(math.min(math.abs(dot), 1.0f)) * 2.0f);

                rotation.Value = angle == 0 ? transform.networkRotation : math.slerp(rotation.Value, transform.networkRotation, math.min(1.0f, angle / serializationRate));
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var syncJob = new PhotonSyncJobHandler()
            {
                serializationRate = PhotonNetwork.SerializationRate,
                time = PhotonNetwork.Time
            };
            return syncJob.Schedule(this, inputDeps);
        }

    }
}