using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;

public class ImageEncoding
{
    [BurstCompile]
    private struct EncodeImageJob : IJob
    {
        [ReadOnly] [DeallocateOnJobCompletion]
        public NativeArray<byte> Input;

        public uint Width;
        public uint Height;
        public int Quality;

        public NativeList<byte> Output;

        public unsafe void Execute()
        {
            // Encode the native array to JPG
            NativeArray<byte> temp = ImageConversion.EncodeNativeArrayToJPG(
                Input, GraphicsFormat.R8G8B8A8_UNorm, Width, Height, (uint)Quality);

            Output.ResizeUninitialized(temp.Length);

            void* internalPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(temp);
            void* outputPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks<byte>(Output);
            UnsafeUtility.MemCpy(outputPtr, internalPtr, temp.Length);

            temp.Dispose();
        }
    }

    public IEnumerator EncodeToJPGAsync(int width, int height, NativeArray<byte> rawData, int quality, System.Action<byte[]> onComplete)
    {
        // Create an output list to hold the encoded JPEG data
        NativeList<byte> outputData = new NativeList<byte>(Allocator.Persistent);

        // Set up the encoding job
        var job = new EncodeImageJob
        {
            Input = rawData,
            Width = (uint)width,
            Height = (uint)height,
            Quality = quality,
            Output = outputData
        };

        // Schedule the job and wait for it to complete
        JobHandle handle = job.Schedule();

        // Wait for the job to complete asynchronously
        yield return new WaitUntil(() => handle.IsCompleted);

        handle.Complete();

        // Convert the NativeList<byte> output to a managed byte array
        NativeArray<byte> nativeArray = outputData.AsArray(); // Using AsArray instead of ToArray
        byte[] result = nativeArray.ToArray();

        // Invoke the callback with the managed byte array
        onComplete?.Invoke(result);

        // Dispose of native resources
        outputData.Dispose();
    }
}
