using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [System.Serializable]
    public class MusicTrack
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    public MusicTrack[] musicTracks;
    private AudioSource[] audioSources;

    void Start()
    {
        // Initialize audio sources and clips
        audioSources = new AudioSource[musicTracks.Length];

        for (int i = 0; i < musicTracks.Length; i++)
        {
            audioSources[i] = gameObject.AddComponent<AudioSource>();
            audioSources[i].clip = musicTracks[i].clip;
            audioSources[i].volume = musicTracks[i].volume;
            audioSources[i].loop = true;
        }

        // Start playing the first track
        PlayTrack(0);
    }

    public void PlayTrack(int trackIndex)
    {
        // Stop all other tracks
        StopAllTracks();

        // Play the selected track
        audioSources[trackIndex].Play();
    }

    public void StopAllTracks()
    {
        // Stop all tracks
        foreach (var audioSource in audioSources)
        {
            audioSource.Stop();
        }
    }

    public void SetVolume(int trackIndex, float volume)
    {
        // Set the volume of the specified track
        audioSources[trackIndex].volume = volume;
    }
}
