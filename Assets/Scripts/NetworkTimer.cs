public class NetworkTimer {
    private float _timer;
    public float minTimeBetweenTicks { get; }
    public int currentTick { get; private set; }
    
    public NetworkTimer(float serverTickRate) {
        minTimeBetweenTicks = 1f / serverTickRate;
    }

    public void Update(float deltaTime) {
        _timer += deltaTime;
    }

    public bool ShouldTick() {
        if (_timer >= minTimeBetweenTicks) {
            _timer -= minTimeBetweenTicks;
            currentTick++;
            return true;
        }

        return false;
    }
}
