cbuffer GlobalBuffer
{
    float _UnscaledTime;

};

void UnscaledTime_float(out float time)
{
    time = _UnscaledTime;
}

