﻿using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm.Activities
{
	/// <summary>Ruminant herd cost </summary>
	/// <summary>This activity will arrange payment of a herd expense such as vet fees</summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(RuminantActivityBuySell))]
	public class RuminantSaleFee: Model
	{
		/// <summary>
		/// Payment style
		/// </summary>
		[Description("Payment style")]
		public PaymentStyleType PaymentStyle { get; set; }

		/// <summary>
		/// Amount
		/// </summary>
		[Description("Amount")]
		public double Amount { get; set; }
	}
}
