
namespace Pathoschild.Stardew.TractorMod.Questable
{
    public enum RestorationState
    {
        NotStarted,
        TalkToLewis,
        TalkToSebastian,
        TalkToLewisAgain,
        WaitingForMailFromRobinDay1,
        WaitingForMailFromRobinDay2,
        BuildTractorGarage,
        WaitingForSebastianDay1,
        WaitingForSebastianDay2,
        TalkToWizard,
        BringStuffToForest,
        BringEngineToSebastian,
        BringEngineToMaru,
        WaitForEngineInstall,
        Complete,
    }

    public static class RestorationStateExtensions
    {
        public static bool IsDerelictInTheFields(this RestorationState _this)
            => _this <= RestorationState.BuildTractorGarage;

        public static bool IsDerelictInTheGarage(this RestorationState _this)
            => _this > RestorationState.BuildTractorGarage && _this < RestorationState.Complete;

        public static bool CanBuildGarage(this RestorationState _this)
            => _this >= RestorationState.BuildTractorGarage;

    }
}
