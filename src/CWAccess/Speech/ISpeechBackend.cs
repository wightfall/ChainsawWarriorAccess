using System;

namespace CWAccess.Speech
{
    internal interface ISpeechBackend : IDisposable
    {
        string Name { get; }

        bool IsAvailable();

        bool Speak(string text, bool interrupt);

        void Stop();
    }
}
