namespace PhobosAutoNav;
internal sealed partial class TorchDriveController
{
    partial void CheckObstacleBurn(Ship candidate,double acceleration,double dt,ref bool allowed) =>
        allowed=Plugin.Service.ObstacleBurnAllowed(candidate,acceleration,dt);
}
