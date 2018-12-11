﻿using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant herd management activity</summary>
    /// <summary>This activity will maintain a breeding herd at the desired levels of age/breeders etc</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyTreeView")]
    [PresenterName("UserInterface.Presenters.PropertyTreePresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs the management of ruminant numbers based upon the current herd filtering. It requires a RuminantActivityBuySell to undertake the purchases and sales.")]
    [Version(1, 0, 1, "Adam Liedloff", "CSIRO", "First implementation of this activity using IAT/NABSA processes")]
    public class RuminantActivityManage: CLEMRuminantActivityBase, IValidatableObject
    {
        /// <summary>
        /// Maximum number of breeders that can be kept
        /// </summary>
        [Category("General", "Breeders")]
        [Description("Maximum number of breeders to be kept")]
        [Required, GreaterThanEqualValue(0)]
        [GreaterThanEqual("MinimumBreedersKept")]
        public int MaximumBreedersKept { get; set; }

        /// <summary>
        /// Minimum number of breeders that can be kept
        /// </summary>
        [Category("General", "Breeders")]
        [Description("Minimum number of breeders to be kept")]
        [Required, GreaterThanEqualValue(0)]
        public int MinimumBreedersKept { get; set; }

        /// <summary>
        /// Maximum breeder age (months) for culling
        /// </summary>
        [Category("General", "Breeders")]
        [Description("Maximum breeder age (months) for culling")]
        [Required, GreaterThanEqualValue(0)]
        public double MaximumBreederAge { get; set; }

        /// <summary>
        /// Proportion of max breeders in single purchase
        /// </summary>
        [Category("General", "Breeders")]
        [Description("Proportion of max breeders in single purchase")]
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, Proportion, GreaterThanValue(0)]
        public double MaximumProportionBreedersPerPurchase { get; set; }

        /// <summary>
        /// The number of 12 month age classes to spread breeder purchases across
        /// </summary>
        [Category("General", "Breeders")]
        [Description("Number of age classes to distribute breeder purchases across")]
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, Range(1,4)]
        public int NumberOfBreederPurchaseAgeClasses { get; set; }

        /// <summary>
        /// Maximum number of breeding sires kept
        /// </summary>
        [Category("General", "Breeding males")]
        [Description("Maximum number of breeding sires kept")]
        [Required, GreaterThanEqualValue(0)]
        public int MaximumSiresKept { get; set; }

        /// <summary>
        /// Maximum bull age (months) for culling
        /// </summary>
        [Category("General", "Breeding males")]
        [Description("Maximum bull age (months) for culling")]
        [Required, GreaterThanEqualValue(0)]
        public double MaximumBullAge { get; set; }

        /// <summary>
        /// Allow natural herd replacement of sires
        /// </summary>
        [Category("General", "Breeding males")]
        [Description("Allow sire replacement from herd")]
        [Required]
        public bool AllowSireReplacement { get; set; }

        /// <summary>
        /// Maximum number of sires in a single purchase
        /// </summary>
        [Category("General", "Breeding males")]
        [Description("Maximum number of sires in a single purchase")]
        [Required, GreaterThanEqualValue(0)]
        public int MaximumSiresPerPurchase { get; set; }

        /// <summary>
        /// Male selling age (months)
        /// </summary>
        [Category("General", "Males")]
        [Description("Male selling age (months)")]
        [Required, GreaterThanEqualValue(0)]
        public double MaleSellingAge { get; set; }

        /// <summary>
        /// Male selling weight (kg)
        /// </summary>
        [Category("General", "Males")]
        [Description("Male selling weight (kg)")]
        [Required]
        public double MaleSellingWeight { get; set; }

        /// <summary>
        /// GrazeFoodStore (paddock) to place purchases in for grazing
        /// </summary>
        [Category("General", "Pasture details")]
        [Description("GrazeFoodStore (paddock) to place purchases in")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(GrazeFoodStore) }, CLEMExtraEntries = new string[] { "Not specified - general yards" })]
        public string GrazeFoodStoreName { get; set; }

        private string grazeStore = "";

        /// <summary>
        /// Minimum pasture (kg/ha) before restocking if placed in paddock
        /// </summary>
        [Category("General", "Pasture details")]
        [Description("Minimum pasture (kg/ha) before restocking if placed in paddock")]
        [Required, Range(0, double.MaxValue, ErrorMessage = "Value must be a greter than or equal to 0")]
        public double MinimumPastureBeforeRestock { get; set; }

        /// <summary>
        /// Perform selling of young females the same as males
        /// </summary>
        [Category("General", "Breeders")]
        [Description("Perform selling of young females the same as males")]
        [Required]
        public bool SellFemalesLikeMales { get; set; }

        /// <summary>
        /// Identify males for sale every time step
        /// </summary>
        [Category("General", "Males")]
        [Description("Identify males for sale every time step")]
        [Required]
        public bool ContinuousMaleSales { get; set; }

        /// <summary>
        /// Store graze 
        /// </summary>
        private GrazeFoodStoreType foodStore;

        /// <summary>
        /// Breed params for this activity
        /// </summary>
        private RuminantType breedParams;

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityManage()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if(12+(NumberOfBreederPurchaseAgeClasses-1)*12 >= MaximumBreederAge)
            {
                string[] memberNames = new string[] { "NumberOfBreederPurchaseAgeClasses" };
                results.Add(new ValidationResult("The number of age classes (12 months each) to spread breeder purchases across will exceed the maximum age of breeders. Reduce number of breeder age classes", memberNames));
            }
            return results;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(false, true);
            breedParams = Resources.GetResourceItem(this, typeof(RuminantHerd), this.PredictedHerdName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as RuminantType;

            // check GrazeFoodStoreExists
            grazeStore = "";
            if(GrazeFoodStoreName != null && !GrazeFoodStoreName.StartsWith("Not specified"))
            {
                grazeStore = GrazeFoodStoreName.Split('.').Last();
                foodStore = Resources.GetResourceItem(this, GrazeFoodStoreName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;
            }

            // check for managed paddocks and warn if animals placed in yards.
            if (grazeStore=="")
            {
                var ah = Apsim.Find(this, typeof(ActivitiesHolder));
                if(Apsim.ChildrenRecursively(ah, typeof(PastureActivityManage)).Count() != 0)
                {
                    Summary.WriteWarning(this, String.Format("Purchased animals are currently placed in yards when managed pasture is available. These animals will not graze until mustered and will require feeding while in yards. This relates to [a={0}]", this.Name));
                }
            }
        }

        /// <summary>An event handler to call for all herd management activities</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalManage")]
        private void OnCLEMAnimalManage(object sender, EventArgs e)
        {
            //List<Ruminant> localHerd = this.CurrentHerd();
            RuminantHerd ruminantHerd = Resources.RuminantHerd();

            // remove only the individuals that are affected by this activity.
            ruminantHerd.PurchaseIndividuals.RemoveAll(a => a.Breed == this.PredictedHerdBreed);

            List<Ruminant> herd = this.CurrentHerd(true);

            // can sell off males any month as per NABSA
            // if we don't need this monthly, then it goes into next if statement with herd declaration
            // NABSA MALES - weaners, 1-2, 2-3 and 3-4 yo, we check for any male weaned and not a breeding sire.
            // check for sell age/weight of young males
            // if SellYoungFemalesLikeMales then all apply to both sexes else only males.
            // SellFemalesLikeMales will grow out excess heifers until age/weight rather than sell immediately.
            if (this.TimingOK || ContinuousMaleSales)
            {
                foreach (var ind in herd.Where(a => a.Weaned & (SellFemalesLikeMales ? true : (a.Gender == Sex.Male)) & (a.Age >= MaleSellingAge || a.Weight >= MaleSellingWeight)))
                {
                    bool sell = true;
                    if (ind.GetType() == typeof(RuminantMale))
                    {
                        // don't sell breeding sires.
                        sell = !((ind as RuminantMale).BreedingSire);
                    }
                    else
                    {
                        // only sell females that were marked as excess
                        sell = ind.Tags.Contains("GrowHeifer");
                    }

                    if (sell)
                    {
                        ind.SaleFlag = HerdChangeReason.AgeWeightSale;
                    }
                }
            }

            // if management month
            if (this.TimingOK)
            {
                bool sufficientFood = true;
                if (foodStore != null)
                {
                    sufficientFood = (foodStore.TonnesPerHectare * 1000) >= MinimumPastureBeforeRestock;
                }

                // check for maximum age (females and males have different cutoffs)
                foreach (var ind in herd.Where(a => a.Age >= ((a.Gender == Sex.Female) ? MaximumBreederAge : MaximumBullAge)))
                {
                    ind.SaleFlag = HerdChangeReason.MaxAgeSale;
                }

                // MALES
                // check for breeder bulls after sale of old individuals and buy/sell
                int numberMaleSiresInHerd = herd.Where(a => a.Gender == Sex.Male & a.SaleFlag == HerdChangeReason.None).Cast<RuminantMale>().Where(a => a.BreedingSire).Count();

                // Number of females
                int numberFemaleBreedingInHerd = herd.Where(a => a.Gender == Sex.Female & a.Age >= a.BreedParams.MinimumAge1stMating & a.SaleFlag == HerdChangeReason.None).Count();
                int numberFemaleTotalInHerd = herd.Where(a => a.Gender == Sex.Female & a.SaleFlag == HerdChangeReason.None).Count();
                int numberFemaleOldInHerd = herd.Where(a => a.Gender == Sex.Female & MaximumBreederAge - a.Age <= 12 & a.SaleFlag == HerdChangeReason.None).Count();
                int numberFemaleHeifersInHerd = herd.Where(a => a.Gender == Sex.Female && ((a as RuminantFemale).IsHeifer & a.SaleFlag == HerdChangeReason.None)).Count();

                if (numberMaleSiresInHerd > MaximumSiresKept)
                {
                    // sell bulls
                    // What rule? oldest first as they may be lost soonest
                    int numberToRemove = numberMaleSiresInHerd - MaximumSiresKept;
                    if (numberToRemove > 0)
                    {
                        foreach (var male in herd.Where(a => a.Gender == Sex.Male).Cast<RuminantMale>().Where(a => a.BreedingSire).OrderByDescending(a => a.Age).Take(numberToRemove))
                        {
                            male.SaleFlag = HerdChangeReason.ExcessBullSale;
                            numberToRemove--;
                            if (numberToRemove == 0) break;
                        }
                    }
                }
                else if(numberMaleSiresInHerd < MaximumSiresKept)
                {
                    if ((foodStore == null) || (sufficientFood))
                    {
                        if (AllowSireReplacement)
                        {
                            // remove young bulls from sale herd to replace breed bulls (not those sold because too old)
                            foreach (RuminantMale male in herd.Where(a => a.Gender == Sex.Male & a.SaleFlag == HerdChangeReason.AgeWeightSale).OrderByDescending(a => a.Weight))
                            {
                                male.SaleFlag = HerdChangeReason.None;
                                male.BreedingSire = true;
                                numberMaleSiresInHerd++;
                                if (numberMaleSiresInHerd >= MaximumSiresKept) break;
                            }
                            // if still insufficent, look into current herd for replacement
                            // remaining males assumed to be too small, so await next time-step
                        }

                        // if still insufficient buy bulls.
                        if (numberMaleSiresInHerd < MaximumSiresKept && (MaximumSiresPerPurchase>0))
                        {
                            // limit by breeders as proportion of max breeders so we don't spend alot on sires when building the herd and females more valuable
                            double propOfBreeders = (double)numberFemaleBreedingInHerd / (double)MaximumBreedersKept;

                            int sires = Convert.ToInt32(Math.Ceiling(Math.Ceiling(MaximumSiresKept * propOfBreeders)));
                            int numberToBuy = Math.Min(MaximumSiresPerPurchase, Math.Max(0, sires - numberMaleSiresInHerd));

                            for (int i = 0; i < numberToBuy; i++)
                            {
                                RuminantMale newbull = new RuminantMale
                                {
                                    Location = grazeStore,
                                    Age = 48,
                                    Breed = this.PredictedHerdBreed,// breedParams.Breed;
                                    HerdName = this.PredictedHerdName,
                                    BreedingSire = true,
                                    BreedParams = breedParams,
                                    Gender = Sex.Male,
                                    ID = 0, // ruminantHerd.NextUniqueID;
                                    Weight = 450,
                                    PreviousWeight = 450,
                                    HighWeight = 450,
                                    SaleFlag = HerdChangeReason.SirePurchase
                                };

                                // add to purchase request list and await purchase in Buy/Sell
                                ruminantHerd.PurchaseIndividuals.Add(newbull);
                            }
                        }
                    }
                }

                // FEMALES
                // Breeding herd traded as heifers only
                int excessHeifers = 0;

                // get the mortality rate for the herd if available or assume zero
                double mortalityRate = 0.0;
                if(herd.Count()>0)
                {
                    mortalityRate = herd.FirstOrDefault().BreedParams.MortalityBase;
                }

                // shortfall between actual and desired numbers of breeders (-ve for shortfall)
                excessHeifers = numberFemaleBreedingInHerd - MaximumBreedersKept;
                // add future cull for age + mortality base%
                int numberDyingInNextYear = Convert.ToInt32((numberFemaleBreedingInHerd - numberFemaleOldInHerd) * mortalityRate) + numberFemaleOldInHerd;
                excessHeifers -= numberDyingInNextYear;

                // account for heifers already in the herd
                excessHeifers += numberFemaleHeifersInHerd;

                // surplus heifers to sell
                if (excessHeifers > 0)
                {
                    foreach (var female in herd.Where(a => a.Gender == Sex.Female &&  (a as RuminantFemale).IsHeifer).Take(excessHeifers))
                    {
                        // if sell like males tag for grow out otherwise mark for sale
                        if (SellFemalesLikeMales)
                        {
                            if (!female.Tags.Contains("GrowHeifer"))
                            {
                                female.Tags.Add("GrowHeifer");
                            }
                        }
                        else
                        {
                            // tag for sale.
                            female.SaleFlag = HerdChangeReason.ExcessHeiferSale;
                        }
                        excessHeifers--;
                        if (excessHeifers == 0) break;
                    }
                }
                else if (excessHeifers < 0)
                {
                    excessHeifers *= -1;
                    if ((foodStore == null) || (sufficientFood))
                    {
                        // remove grow out heifers from grow out herd to replace breeders
                        if (SellFemalesLikeMales)
                        {
                            foreach (Ruminant female in herd.Where(a => a.Tags.Contains("GrowHeifer")).OrderByDescending(a => a.Age))
                            {
                                female.Tags.Remove("GrowHeifer");
                                excessHeifers--;
                                if (excessHeifers == 0) break;
                            }
                        }

                        // remove young females from sale herd to replace breeders (not those sold because too old)
                        foreach (RuminantFemale female in herd.Where(a => a.Gender == Sex.Female & (a.SaleFlag == HerdChangeReason.AgeWeightSale | a.SaleFlag == HerdChangeReason.ExcessHeiferSale)).OrderByDescending(a => a.Age))
                        {
                            female.SaleFlag = HerdChangeReason.None;
                            excessHeifers--;
                            if (excessHeifers == 0) break;
                        }

                        // if still insufficient buy breeders.
                        if (excessHeifers > 0 & (MaximumProportionBreedersPerPurchase > 0))
                        {
                            int ageOfHeifer = 0;

                            // buy mortality base% more to account for deaths before these individuals grow to breeding age
                            // ceiling MaxProportion*MaxBreeders ensures at least one individual will set as limit. Important with small breeder herds.
                            // minimum of (max kept x prop in single purchase) and (the number needed + annual mortality)
                            int numberToBuy = Math.Min(Convert.ToInt32(Math.Ceiling(MaximumProportionBreedersPerPurchase*MaximumBreedersKept)), Math.Max(0, Convert.ToInt32(excessHeifers * (1+ mortalityRate))));

                            int numberPerPurchaseCohort = Convert.ToInt32(numberToBuy / Convert.ToDouble(NumberOfBreederPurchaseAgeClasses));

                            for (int j = 0; j < NumberOfBreederPurchaseAgeClasses; j++)
                            {
                                // ensure rounding errors allow total number to be purchase by upping cat 1 (12 month olds)
                                int numberThisCohort = numberPerPurchaseCohort;
                                if(j == 0)
                                {
                                    numberThisCohort = numberToBuy - ((NumberOfBreederPurchaseAgeClasses - 1) * numberPerPurchaseCohort);
                                }
                                ageOfHeifer = 12+(j*12);
                                for (int i = 0; i < numberThisCohort; i++)
                                {
                                    RuminantFemale newheifer = new RuminantFemale
                                    {
                                        Location = grazeStore,
                                        Age = ageOfHeifer,
                                        Breed = this.PredictedHerdBreed,
                                        HerdName = this.PredictedHerdName,
                                        BreedParams = breedParams,
                                        Gender = Sex.Female,
                                        ID = 0,
                                        SaleFlag = HerdChangeReason.HeiferPurchase
                                    };
                                    // calculate normalised weight based on age.
                                    double weight = newheifer.NormalisedAnimalWeight;
                                    newheifer.Weight = weight;
                                    newheifer.PreviousWeight = weight;
                                    newheifer.HighWeight = weight;

                                    // this individual must be weaned to be a heifer and permitted to start breeding.
                                    newheifer.Wean();
                                    // add to purchase request list and await purchase in Buy/Sell
                                    ruminantHerd.PurchaseIndividuals.Add(newheifer);
                                }
                            }
                        }
                    }
                }
                // Breeders themselves don't get sold. Trading is with Heifers
                // Breeders can be sold in seasonal and ENSO destocking.
                // sell breeders
                // What rule? oldest first as they may be lost soonest
                // should keep pregnant females... and young...
                // this will currently remove pregnant females and females with suckling calf
            }
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            return;
        }


        /// <summary>
        /// Determine the labour required for this activity based on LabourRequired items in tree
        /// </summary>
        /// <param name="Requirement">Labour requirement model</param>
        /// <returns></returns>
        public override double GetDaysLabourRequired(LabourRequirement Requirement)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            if (ResourceShortfallOccurred != null)
                ResourceShortfallOccurred(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="FormatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool FormatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activitycontentlight\">";
            html += "\n<div class=\"labournote\">Breeding females</div>";
            html += "\n<div class=\"activityentry\">";
            html += "The herd will be maintained ";
            if (MinimumBreedersKept == MaximumBreedersKept)
            {
                html += "at <span class=\"setvalue\">" + MinimumBreedersKept.ToString("#,###") + "</span> individual"+((MinimumBreedersKept!=1)?"s":"") ;
            }
            else
            {
                html += ((MinimumBreedersKept > 0) ? "between <span class=\"setvalue\">" + MinimumBreedersKept.ToString("#,###") + "</span> and " : "below ") + "<span class=\"setvalue\">" + MaximumBreedersKept.ToString("#,###") + "</span>";
            }
            html += "</div>";
            html += "\n<div class=\"activityentry\">";
            html += "Individuals will be sold when over <span class=\"setvalue\">" + MaximumBreederAge.ToString("###") + "</span> months old";
            html += "</div>";
            html += "\n<div class=\"activityentry\">";
            html += "A maximum of <span class=\"setvalue\">" + MaximumProportionBreedersPerPurchase.ToString("#0.##%") + "</span> of the Maximum Breeders Kept can be purchased in a single transaction";
            html += "</div>";
            html += "</div>";

            html += "\n<div class=\"activitycontentlight\">";
            html += "\n<div class=\"labournote\">Breeding males (sires/rams etc)</div>";
            html += "\n<div class=\"activityentry\">";
            if (MaximumSiresKept == 0)
            {
                html += "No breeding sires will be kept";
            }
            else
            {
                html += "A maximum of <span class=\"setvalue\">" + MaximumSiresKept.ToString("#,###") + "</span> will be kept";
            }
            html += "</div>";
            html += "\n<div class=\"activityentry\">";
            html += "Individuals will be sold when over <span class=\"setvalue\">" + MaximumBullAge.ToString("###") + "</span> months old";
            html += "</div>";
            html += "</div>";

            html += "\n<div class=\"activitycontentlight\">";
            html += "\n<div class=\"labournote\">General herd</div>";
            if (MaleSellingAge + MaleSellingWeight > 0)
            {
                html += "\n<div class=\"activityentry\">";
                html += "Males will be sold when <span class=\"setvalue\">" + MaleSellingAge.ToString("###") + "</span> months old or <span class=\"setvalue\">" + MaleSellingWeight.ToString("#,###") + "</span> kg";
                html += "</div>";
                if (ContinuousMaleSales)
                {
                    html += "\n<div class=\"activityentry\">";
                    html += "Animals will be sold in any month";
                    html += "</div>";
                }
                else
                {
                    html += "\n<div class=\"activityentry\">";
                    html += "Animals will be sold only when activity is due";
                    html += "</div>";
                }
                if (SellFemalesLikeMales)
                {
                    html += "\n<div class=\"activityentry\">";
                    html += "Females will be sold the same as males";
                    html += "</div>";
                }
            }
            else
            {
                html += "\n<div class=\"activityentry\">";
                html += "There are no age or weight sales of individuals.";
                html += "</div>";
            }
            html += "</div>";

            html += "\n<div class=\"activityentry\">";
            html += "Purchased individuals will be placed in ";
            if (GrazeFoodStoreName == null || GrazeFoodStoreName == "")
            {
                html += "<span class=\"resourcelink\">General yards</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + GrazeFoodStoreName + "</span>";
            }
            html += "</div>";

            return html;
        }
    }
}
