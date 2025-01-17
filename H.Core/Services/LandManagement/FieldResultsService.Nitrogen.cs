﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using H.Core.Emissions.Results;
using H.Core.Enumerations;
using H.Core.Models;
using H.Core.Models.LandManagement.Fields;
using H.Core.Providers.Soil;
using H.Core.Services.Animals;
using H.Infrastructure;

namespace H.Core.Services.LandManagement
{
    public partial class FieldResultsService
    {
        #region Public Methods

        /// <summary>
        /// Equation 4.6.1-2
        ///
        /// (kg N)
        /// </summary>
        public double CalculateTotalManureNitrogenAppliedToAllFields(FarmEmissionResults farmEmissionResults,
            List<CropViewItem> itemsInYear)
        {
            var result = 0d;

            foreach (var cropViewItem in itemsInYear)
            {
                var appliedNitrogenFromManure = cropViewItem.GetTotalManureNitrogenAppliedFromLivestockInYear();

                result += appliedNitrogenFromManure;
            }

            return result;
        }

        /// <summary>
        /// Equation 4.6.1-4
        /// </summary>
        public double CalculateTotalNitrogenFromLandManureRemaining(
            double totalManureAvailableForLandApplication,
            double totalManureAlreadyAppliedToFields,
            double totalManureExported)
        {
            return totalManureAvailableForLandApplication - totalManureAlreadyAppliedToFields - totalManureExported;
        }

        /// <summary>
        /// Equation 4.6.1-5
        /// </summary>
        public double CalculateTotalEmissionsFromRemainingManureThatIsAppliedToAllFields(
            double weightedEmissionFactor,
            double totalNitrogenFromLandManureRemaining)
        {

            var result = totalNitrogenFromLandManureRemaining * weightedEmissionFactor;

            return result;
        }

        /// <summary>
        /// Calculate amount of nitrogen input from all manure applications in a year
        /// </summary>
        /// <returns>The amount of nitrogen input during the year (kg N)</returns>
        public double CalculateManureNitrogenInput(
            CropViewItem cropViewItem,
            Farm farm)
        {
            var result = 0.0;

            foreach (var manureApplicationViewItem in cropViewItem.ManureApplicationViewItems)
            {
                var manureCompositionData = manureApplicationViewItem.DefaultManureCompositionData;

                var fractionOfNitrogen = manureCompositionData.NitrogenContent;

                var amountOfNitrogen = _icbmSoilCarbonCalculator.CalculateAmountOfNitrogenAppliedFromManure(
                    manureAmount: manureApplicationViewItem.AmountOfManureAppliedPerHectare,
                    fractionOfNitrogen);

                result += amountOfNitrogen;
            }

            return Math.Round(result, DefaultNumberOfDecimalPlaces);
        }

        /// <summary>
        /// Determines the amount of N fertilizer required for the specified crop type and yield
        /// </summary>
        public double CalculateRequiredNitrogenFertilizer(
            Farm farm,
            CropViewItem viewItem,
            FertilizerApplicationViewItem fertilizerApplicationViewItem)
        {
            _icbmSoilCarbonCalculator.SetCarbonInputs(
                previousYearViewItem: null,
                currentYearViewItem: viewItem,
                nextYearViewItem: null,
                farm: farm);

            var nitrogenContentOfGrainReturnedToSoil = _singleYearNitrogenEmissionsCalculator.CalculateGrainNitrogenTotal(
                carbonInputFromAgriculturalProduct: viewItem.PlantCarbonInAgriculturalProduct,
                nitrogenConcentrationInProduct: viewItem.NitrogenContentInProduct);

            var nitrogenContentOfStrawReturnedToSoil = _singleYearNitrogenEmissionsCalculator.CalculateStrawNitrogen(
                carbonInputFromStraw: viewItem.CarbonInputFromStraw,
                nitrogenConcentrationInStraw: viewItem.NitrogenContentInStraw);

            var nitrogenContentOfRootReturnedToSoil = _singleYearNitrogenEmissionsCalculator.CalculateRootNitrogen(
                carbonInputFromRoots: viewItem.CarbonInputFromRoots,
                nitrogenConcentrationInRoots: viewItem.NitrogenContentInRoots);

            var nitrogenContentOfExtrarootReturnedToSoil = _singleYearNitrogenEmissionsCalculator.CalculateExtrarootNitrogen(
                carbonInputFromExtraroots: viewItem.CarbonInputFromExtraroots,
                nitrogenConcentrationInExtraroots: viewItem.NitrogenContentInExtraroot);

            var isLeguminousCrop = viewItem.CropType.IsLeguminousCoverCrop() || viewItem.CropType.IsPulseCrop();

            var syntheticFertilizerApplied = _singleYearNitrogenEmissionsCalculator.CalculateSyntheticFertilizerApplied(
                nitrogenContentOfGrainReturnedToSoil: nitrogenContentOfGrainReturnedToSoil,
                nitrogenContentOfStrawReturnedToSoil: nitrogenContentOfStrawReturnedToSoil,
                nitrogenContentOfRootReturnedToSoil: nitrogenContentOfRootReturnedToSoil,
                nitrogenContentOfExtrarootReturnedToSoil: nitrogenContentOfExtrarootReturnedToSoil,
                fertilizerEfficiencyFraction: fertilizerApplicationViewItem.FertilizerEfficiencyFraction,
                soilTestN: viewItem.SoilTestNitrogen,
                isNitrogenFixingCrop: isLeguminousCrop,
                nitrogenFixationAmount: viewItem.NitrogenFixation,
                atmosphericNitrogenDeposition: viewItem.NitrogenDepositionAmount);

            return syntheticFertilizerApplied;
        }

        /// <summary>
        /// Equation 2.5.2-6
        ///
        /// Calculates the amount of the fertilizer blend needed to support the yield that was input.This considers the amount of nitrogen uptake by the plant and then
        /// converts that value into an amount of fertilizer blend/product
        /// </summary>
        /// <returns>The amount of product required (kg product ha^-1)</returns>
        public double CalculateAmountOfProductRequired(
            Farm farm,
            CropViewItem viewItem,
            FertilizerApplicationViewItem fertilizerApplicationViewItem)
        {
            var requiredNitrogen = CalculateRequiredNitrogenFertilizer(
                farm: farm,
                viewItem: viewItem,
                fertilizerApplicationViewItem: fertilizerApplicationViewItem);

            // If blend is custom, default N value will be zero and so we can't calculate the amount of product required
            if (fertilizerApplicationViewItem.FertilizerBlendData.PercentageNitrogen == 0)
            {
                return 0;
            }

            // Need to convert to amount of fertilizer product from required nitrogen
            var requiredAmountOfProduct = (requiredNitrogen / (fertilizerApplicationViewItem.FertilizerBlendData.PercentageNitrogen / 100));

            return requiredAmountOfProduct;
        }

        #endregion

        #region Private Methods

        #endregion
    }
}