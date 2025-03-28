//Created by ProjectDataSeederCreator at 3/27/2025 12:24:30 AM

using System;
using DatabaseToolsShared;
using Microsoft.Extensions.Logging;

namespace SupportToolsServerDataSeeder;

public /*open*/ class ProjectDataSeeder : DataSeeder
{
    protected ProjectDataSeeder(ILogger<ProjectDataSeeder> logger, DataSeedersFabric dataSeedersFabric, bool checkOnly)
        : base(logger, dataSeedersFabric, checkOnly)
    {
    }

    public override bool SeedData()
    {
        if (!base.SeedData())
            return false;

        var seederFabric = (GrgDataSeedersFabric)DataSeedersFabric;

        Logger.LogInformation("Seed Project Data Started");

        Logger.LogInformation("Seeding ActantGrammarCases");

        //1 ActantGrammarCases
        if (!Use(seederFabric.CreateActantGrammarCasesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding ActantGroups");

        //2 ActantGroups
        if (!Use(seederFabric.CreateActantGroupsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding ActantPositions");

        //3 ActantPositions
        if (!Use(seederFabric.CreateActantPositionsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding ActantTypes");

        //4 ActantTypes
        if (!Use(seederFabric.CreateActantTypesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding AlphabetsVowels");

        //5 AlphabetsVowels
        if (!Use(seederFabric.CreateAlphabetsVowelsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding BasePhoneticsCombs");

        //6 BasePhoneticsCombs
        if (!Use(seederFabric.CreateBasePhoneticsCombsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding Classifiers");

        //7 Classifiers
        if (!Use(seederFabric.CreateClassifiersSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding Corrections");

        //8 Corrections
        if (!Use(seederFabric.CreateCorrectionsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding FreeMorphFormulas");

        //9 FreeMorphFormulas
        if (!Use(seederFabric.CreateFreeMorphFormulasSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding GrammarCases");

        //10 GrammarCases
        if (!Use(seederFabric.CreateGrammarCasesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding IssueKinds");

        //11 IssueKinds
        if (!Use(seederFabric.CreateIssueKindsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding IssuePriorities");

        //12 IssuePriorities
        if (!Use(seederFabric.CreateIssuePrioritiesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding IssueStatuses");

        //13 IssueStatuses
        if (!Use(seederFabric.CreateIssueStatusesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding MorphemeGroups");

        //14 MorphemeGroups
        if (!Use(seederFabric.CreateMorphemeGroupsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding NounNumbers");

        //15 NounNumbers
        if (!Use(seederFabric.CreateNounNumbersSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding NounParadigms");

        //16 NounParadigms
        if (!Use(seederFabric.CreateNounParadigmsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding PersonVariabilityCombinations");

        //17 PersonVariabilityCombinations
        if (!Use(seederFabric.CreatePersonVariabilityCombinationsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding PersonVariabilityTypes");

        //18 PersonVariabilityTypes
        if (!Use(seederFabric.CreatePersonVariabilityTypesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding PhoneticsOptions");

        //19 PhoneticsOptions
        if (!Use(seederFabric.CreatePhoneticsOptionsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding PhoneticsTypes");

        //20 PhoneticsTypes
        if (!Use(seederFabric.CreatePhoneticsTypesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding Pronouns");

        //21 Pronouns
        if (!Use(seederFabric.CreatePronounsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding RecordStatuses");

        //22 RecordStatuses
        if (!Use(seederFabric.CreateRecordStatusesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbNumbers");

        //23 VerbNumbers
        if (!Use(seederFabric.CreateVerbNumbersSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbParadigms");

        //24 VerbParadigms
        if (!Use(seederFabric.CreateVerbParadigmsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbPersons");

        //25 VerbPersons
        if (!Use(seederFabric.CreateVerbPersonsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbPluralityTypes");

        //26 VerbPluralityTypes
        if (!Use(seederFabric.CreateVerbPluralityTypesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbRowFilters");

        //27 VerbRowFilters
        if (!Use(seederFabric.CreateVerbRowFiltersSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbSeries");

        //28 VerbSeries
        if (!Use(seederFabric.CreateVerbSeriesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbTransitions");

        //29 VerbTransitions
        if (!Use(seederFabric.CreateVerbTransitionsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding ActantGrammarCasesByActantTypes");

        //30 ActantGrammarCasesByActantTypes
        if (!Use(seederFabric.CreateActantGrammarCasesByActantTypesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding ActantPronounsByGrammarCases");

        //31 ActantPronounsByGrammarCases
        if (!Use(seederFabric.CreateActantPronounsByGrammarCasesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding DerivationTypes");

        //32 DerivationTypes
        if (!Use(seederFabric.CreateDerivationTypesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding DominantActants");

        //33 DominantActants
        if (!Use(seederFabric.CreateDominantActantsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding InflectionVerbCompositions");

        //34 InflectionVerbCompositions
        if (!Use(seederFabric.CreateInflectionVerbCompositionsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding Issues");

        //35 Issues
        if (!Use(seederFabric.CreateIssuesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding MorphemeRanges");

        //36 MorphemeRanges
        if (!Use(seederFabric.CreateMorphemeRangesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding NounParadigmRows");

        //37 NounParadigmRows
        if (!Use(seederFabric.CreateNounParadigmRowsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding NounSamplePositions");

        //38 NounSamplePositions
        if (!Use(seederFabric.CreateNounSamplePositionsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding PersonVariabilityCombinationDetails");

        //39 PersonVariabilityCombinationDetails
        if (!Use(seederFabric.CreatePersonVariabilityCombinationDetailsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding PersonVariabilityDetails");

        //40 PersonVariabilityDetails
        if (!Use(seederFabric.CreatePersonVariabilityDetailsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding PhoneticsChanges");

        //41 PhoneticsChanges
        if (!Use(seederFabric.CreatePhoneticsChangesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding PhoneticsOptionDetails");

        //42 PhoneticsOptionDetails
        if (!Use(seederFabric.CreatePhoneticsOptionDetailsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding PhoneticsTypeProhibitions");

        //43 PhoneticsTypeProhibitions
        if (!Use(seederFabric.CreatePhoneticsTypeProhibitionsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbPersonMarkerParadigms");

        //44 VerbPersonMarkerParadigms
        if (!Use(seederFabric.CreateVerbPersonMarkerParadigmsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbPluralityTypeChanges");

        //45 VerbPluralityTypeChanges
        if (!Use(seederFabric.CreateVerbPluralityTypeChangesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbRows");

        //46 VerbRows
        if (!Use(seederFabric.CreateVerbRowsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbTypes");

        //47 VerbTypes
        if (!Use(seederFabric.CreateVerbTypesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding ActantTypesByVerbTypes");

        //48 ActantTypesByVerbTypes
        if (!Use(seederFabric.CreateActantTypesByVerbTypesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding BasePhoneticsCombDetails");

        //49 BasePhoneticsCombDetails
        if (!Use(seederFabric.CreateBasePhoneticsCombDetailsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding DerivationFormulas");

        //50 DerivationFormulas
        if (!Use(seederFabric.CreateDerivationFormulasSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding InflectionTypes");

        //51 InflectionTypes
        if (!Use(seederFabric.CreateInflectionTypesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding IssueDetailByInflectionVerbCompositions");

        //52 IssueDetailByInflectionVerbCompositions
        if (!Use(seederFabric.CreateIssueDetailByInflectionVerbCompositionsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding IssueDetails");

        //53 IssueDetails
        if (!Use(seederFabric.CreateIssueDetailsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding MorphemeRangesByDerivationTypes");

        //54 MorphemeRangesByDerivationTypes
        if (!Use(seederFabric.CreateMorphemeRangesByDerivationTypesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding Morphemes");

        //55 Morphemes
        if (!Use(seederFabric.CreateMorphemesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding PluralityChangesByVerbTypes");

        //56 PluralityChangesByVerbTypes
        if (!Use(seederFabric.CreatePluralityChangesByVerbTypesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbParadigmRows");

        //57 VerbParadigmRows
        if (!Use(seederFabric.CreateVerbParadigmRowsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbPersonMarkerParadigmChanges");

        //58 VerbPersonMarkerParadigmChanges
        if (!Use(seederFabric.CreateVerbPersonMarkerParadigmChangesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbPersonMarkerParadigmRows");

        //59 VerbPersonMarkerParadigmRows
        if (!Use(seederFabric.CreateVerbPersonMarkerParadigmRowsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbRowFilterDetails");

        //60 VerbRowFilterDetails
        if (!Use(seederFabric.CreateVerbRowFilterDetailsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbSamplePositions");

        //61 VerbSamplePositions
        if (!Use(seederFabric.CreateVerbSamplePositionsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding DerivationBranches");

        //62 DerivationBranches
        if (!Use(seederFabric.CreateDerivationBranchesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding DerivationFormulaDetails");

        //63 DerivationFormulaDetails
        if (!Use(seederFabric.CreateDerivationFormulaDetailsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding FreeMorphFormulaDetails");

        //64 FreeMorphFormulaDetails
        if (!Use(seederFabric.CreateFreeMorphFormulaDetailsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding InflectionBlocks");

        //65 InflectionBlocks
        if (!Use(seederFabric.CreateInflectionBlocksSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding Inflections");

        //66 Inflections
        if (!Use(seederFabric.CreateInflectionsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding MorphPhoneticsOccasions");

        //67 MorphPhoneticsOccasions
        if (!Use(seederFabric.CreateMorphPhoneticsOccasionsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding MorphPhoneticsOptions");

        //68 MorphPhoneticsOptions
        if (!Use(seederFabric.CreateMorphPhoneticsOptionsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding NounParadigmFormulaDetails");

        //69 NounParadigmFormulaDetails
        if (!Use(seederFabric.CreateNounParadigmFormulaDetailsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbParadigmFormulaDetails");

        //70 VerbParadigmFormulaDetails
        if (!Use(seederFabric.CreateVerbParadigmFormulaDetailsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbPersonMarkerFormulaDetails");

        //71 VerbPersonMarkerFormulaDetails
        if (!Use(seederFabric.CreateVerbPersonMarkerFormulaDetailsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbSamplePersonVariabilities");

        //72 VerbSamplePersonVariabilities
        if (!Use(seederFabric.CreateVerbSamplePersonVariabilitiesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding DerivationBranchFreeMorphemes");

        //73 DerivationBranchFreeMorphemes
        if (!Use(seederFabric.CreateDerivationBranchFreeMorphemesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding DerivationBranchPhoneticCapabilities");

        //74 DerivationBranchPhoneticCapabilities
        if (!Use(seederFabric.CreateDerivationBranchPhoneticCapabilitiesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding DerivationPredecessors");

        //75 DerivationPredecessors
        if (!Use(seederFabric.CreateDerivationPredecessorsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding InflectionPredecessors");

        //76 InflectionPredecessors
        if (!Use(seederFabric.CreateInflectionPredecessorsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding InflectionVerbCompositionPredecessors");

        //77 InflectionVerbCompositionPredecessors
        if (!Use(seederFabric.CreateInflectionVerbCompositionPredecessorsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding IssueDetailsByDerivationBranches");

        //78 IssueDetailsByDerivationBranches
        if (!Use(seederFabric.CreateIssueDetailsByDerivationBranchesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding IssueDetailsByInflections");

        //79 IssueDetailsByInflections
        if (!Use(seederFabric.CreateIssueDetailsByInflectionsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding MorphemeRangesByInflectionBlocks");

        //80 MorphemeRangesByInflectionBlocks
        if (!Use(seederFabric.CreateMorphemeRangesByInflectionBlocksSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding NounInflections");

        //81 NounInflections
        if (!Use(seederFabric.CreateNounInflectionsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding Roots");

        //82 Roots
        if (!Use(seederFabric.CreateRootsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbInflections");

        //83 VerbInflections
        if (!Use(seederFabric.CreateVerbInflectionsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding IssueDetailsByRoots");

        //84 IssueDetailsByRoots
        if (!Use(seederFabric.CreateIssueDetailsByRootsSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbInflectionFreeMorphemes");

        //85 VerbInflectionFreeMorphemes
        if (!Use(seederFabric.CreateVerbInflectionFreeMorphemesSeeder()))
        {
            return false;
        }

        Logger.LogInformation("Seeding VerbInflectionPersonVariabilities");

        //86 VerbInflectionPersonVariabilities
        if (!Use(seederFabric.CreateVerbInflectionPersonVariabilitiesSeeder()))
        {
            return false;
        }

        Console.WriteLine("DataSeederCreator.Run Finished");
        return true;
    }
}