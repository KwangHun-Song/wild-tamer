namespace Base
{
    public interface ISoundManager
    {
        void PlayBGM(string clipName);
        void StopBGM();
        void PlaySFX(string clipName);
        float BGMVolume { get; set; }
        float SFXVolume { get; set; }
        bool IsMuted { get; set; }
    }
}
