using System.Collections.Generic;
public static class RelicDatabase
{
    private static List<Relic> relics;
    public static bool initialized => relics != null;
    public static void Load()
    {
        relics = new List<Relic>
        {
            new EPRelic(),
            new GCRelic(),
            new GMRelic(),
            new ITIRelic(),
            new MECARelic(),
            new MRIERelic(),
            new AIRelic(),
            new PERFRelic(),
            new CFIRelic(),
            new RestChargesRelic(),
            new RestHealRelic(),
            new PortableWorkshopRelic(),
            new SafetyNetRelic(),
            new PocketBatteryRelic(),
            new FieldRationsRelic(),
            new LedgerRelic(),
            new ReinforcedBuckleRelic(),
            new OverclockRelic(),
            new ReactivePlatingRelic(),
            new StudyNotesRelic(),
            new ShatterPulseRelic(),
            new ArtifactCharmRelic(),
            new MeditativeFocusRelic(),
            new SpeedCadenceRelic(),
            new StrengthCadenceRelic(),
            new DexterityCadenceRelic(),
            new CounterStanceRelic(),
            new ScholarSequenceRelic(),
            new SurgeSequenceRelic(),
            new UltimateStrengthRelic(),
            new UltimateDexterityRelic(),
            new MoraleRelic(),
            new ThornsRelic(),
            new ISRelic(),
            new DSRelic(),
            new FastWorkerRelic(),
            new SolarCoreRelic(),
            new LastStandRelic(),
            new MirrorShellRelic(),
            new TempoHourglassRelic(),
            new StrongbackRelic(),
            new FortifiedMomentumRelic(),
            new TwelfthStrikeRelic(),
            new NineSkillsBatteryRelic(),
            new PowerCompletionRelic(),
            new CrownOfAshesRelic(),
            new InfernalContractRelic(),
            new TitanHeartRelic(),
            new SovereignHaloRelic(),
            new SolarCageRelic(),
            new GlassCannonRelic(),
            new WarpedChronometerRelic(),
            new BloodPactRelic(),
            new BlackSunRelic(),
            new RunicAegisRelic(),
            new DreadBellRelic(),
            new LeviathanCoreRelic(),
            new ImmutableSpineRelic(),
            new FrenziedIdolRelic(),
            new BrutalConvictionRelic(),
            new StatusNullificationRelic(),
            new TemporalAnchorRelic(),
            new JuryRelic()
        };
    }
    public static List<Relic> GetRelicsByRarity(RelicRarity rarity)
    {
        return relics.FindAll(r => r.rarity == rarity);
    }
}