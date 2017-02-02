// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Azure.Commands.Automation.Common;
using Microsoft.Azure.Commands.Automation.Properties;
using System.Management.Automation;
using System.Security.Permissions;
using Microsoft.Azure.Management.Automation.Models;
using Schedule = Microsoft.Azure.Commands.Automation.Model.Schedule;
using ScheduleFrequency = Microsoft.Azure.Commands.Automation.Model.ScheduleFrequency;

namespace Microsoft.Azure.Commands.Automation.Cmdlet
{
    /// <summary>
    /// Sets an azure automation schedule.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "AzureRmAutomationSchedule", DefaultParameterSetName = AutomationCmdletParameterSets.ByName)]
    [OutputType(typeof(Schedule))]
    public class SetAzureAutomationSchedule : AzureAutomationBaseCmdlet
    {
        /// <summary>
        /// Gets or sets the schedule name.
        /// </summary>
        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true,
            HelpMessage = "The schedule name.")]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the indicator whether the schedule is enabled.
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true,
            HelpMessage = "Specifies whether the schedule is enabled. If a schedule is disabled, any runbooks using it will not run on the schedule until it is enabled.")]
        public bool? IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the schedule description.
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true,
            HelpMessage = "The schedule description.")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the schedule start time.
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The schedule start time.")]
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// Gets or sets the schedule days of the week.
        /// </summary>
        [Parameter(ParameterSetName = AutomationCmdletParameterSets.ByWeekly, Mandatory = false, HelpMessage = "The list of days of week for the weekly schedule.")]
        public System.DayOfWeek[] DaysOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the schedule days of the month.
        /// </summary>
        [Parameter(ParameterSetName = AutomationCmdletParameterSets.ByMonthlyDaysOfMonth, Mandatory = false, HelpMessage = "The list of days of month for the monthly schedule.")]
        public DaysOfMonth[] DaysOfMonth { get; set; }

        /// <summary>
        /// Gets or sets the schedule day of the week.
        /// </summary>
        [Parameter(ParameterSetName = AutomationCmdletParameterSets.ByMonthlyDayOfWeek, Mandatory = false, HelpMessage = "The day of week for the monthly occurrence.")]
        public System.DayOfWeek? DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the schedule day of the week.
        /// </summary>
        [Parameter(ParameterSetName = AutomationCmdletParameterSets.ByMonthlyDayOfWeek, Mandatory = false, HelpMessage = "The Occurrence of the week within the month.")]
        public DayOfWeekOccurrence DayOfWeekOccurrence { get; set; }

        /// <summary>
        /// Gets or sets the switch parameter to create a one time schedule.
        /// </summary>
        [Parameter(ParameterSetName = AutomationCmdletParameterSets.ByOneTime, Mandatory = true, HelpMessage = "To create a one time schedule.")]
        public SwitchParameter OneTime { get; set; }

        /// <summary>
        /// Gets or sets the schedule expiry time.
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The schedule expiry time.")]
        public DateTimeOffset? ExpiryTime { get; set; }

        /// <summary>
        /// Gets or sets the daily schedule day interval.
        /// </summary>
        [Parameter(ParameterSetName = AutomationCmdletParameterSets.ByDaily, Mandatory = true, HelpMessage = "The daily schedule day interval.")]
        [ValidateRange(1, byte.MaxValue)]
        public byte DayInterval { get; set; }

        /// <summary>
        /// Gets or sets the hourly schedule hour interval.
        /// </summary>
        [Parameter(ParameterSetName = AutomationCmdletParameterSets.ByHourly, Mandatory = true, HelpMessage = "The hourly schedule hour interval.")]
        [ValidateRange(1, byte.MaxValue)]
        public byte HourInterval { get; set; }

        /// <summary>
        /// Gets or sets the weekly schedule week interval.
        /// </summary>
        [Parameter(ParameterSetName = AutomationCmdletParameterSets.ByWeekly, Mandatory = true, HelpMessage = "The weekly schedule week interval.")]
        [ValidateRange(1, byte.MaxValue)]
        public byte WeekInterval { get; set; }

        /// <summary>
        /// Gets or sets the weekly schedule week interval.
        /// </summary>
        [Parameter(ParameterSetName = AutomationCmdletParameterSets.ByMonthlyDaysOfMonth, Mandatory = true, HelpMessage = "The monthly schedule month interval.")]
        [Parameter(ParameterSetName = AutomationCmdletParameterSets.ByMonthlyDayOfWeek, Mandatory = true, HelpMessage = "The monthly schedule month interval.")]
        [ValidateRange(1, byte.MaxValue)]
        public byte MonthInterval { get; set; }

        /// <summary>
        /// Gets or sets the schedule time zone.
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The schedule time zone.")]
        public string TimeZone { get; set; }

        /// <summary>
        /// Execute this cmdlet.
        /// </summary>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        protected override void AutomationProcessRecord()
        {
            var recurrence = this.GetRecurrenceInfo();
            var schedule = this.AutomationClient.UpdateSchedule(
                this.ResourceGroupName, 
                this.AutomationAccountName, 
                this.Name, 
                this.IsEnabled, 
                this.Description,
                this.StartTime,
                this.ExpiryTime,
                recurrence.Interval,
                recurrence.Frequency == null ? null : recurrence.Frequency.ToString(),
                recurrence.AdvancedSchedule,
                this.TimeZone);
            this.WriteObject(schedule);
        }

        /// <summary>
        /// Gets the recurrence information for this cmdlet.
        /// </summary>
        /// <returns>An instance of <see cref="RecurrenceInfo"/> filled with the data passed to the cmdlet.</returns>
        private RecurrenceInfo GetRecurrenceInfo()
        {
            RecurrenceInfo recurrence;
            switch (this.ParameterSetName)
            {
                case AutomationCmdletParameterSets.ByOneTime:
                    recurrence = this.GetOneTimeRecurrence();
                    break;
                case AutomationCmdletParameterSets.ByDaily:
                    recurrence = this.GetDailyRecurrence();
                    break;
                case AutomationCmdletParameterSets.ByHourly:
                    recurrence = this.GetHourlyRecurrence();
                    break;
                case AutomationCmdletParameterSets.ByWeekly:
                    recurrence = this.GetWeeklyRecurrence();
                    break;
                case AutomationCmdletParameterSets.ByMonthlyDayOfWeek:
                    recurrence = this.GetMonthlyDayOfWeekRecurrence();
                    break;
                case AutomationCmdletParameterSets.ByMonthlyDaysOfMonth:
                    recurrence = this.GetMonthlyDaysOfMonthRecurrence();
                    break;
                default:
                    recurrence = new RecurrenceInfo();
                    break;
            }
            return recurrence;
        }

        private RecurrenceInfo GetOneTimeRecurrence()
        {
            return new RecurrenceInfo
            {
                Frequency = ScheduleFrequency.Onetime
            };
        }

        private RecurrenceInfo GetHourlyRecurrence()
        {
            return new RecurrenceInfo
            {
                Frequency = ScheduleFrequency.Hour,
                Interval = this.HourInterval
            };
        }

        private RecurrenceInfo GetDailyRecurrence()
        {
            return new RecurrenceInfo
            {
                Frequency = ScheduleFrequency.Day,
                Interval = this.DayInterval
            };
        }

        private RecurrenceInfo GetWeeklyRecurrence()
        {
            var recurrence = new RecurrenceInfo
            {
                Frequency = ScheduleFrequency.Week,
                Interval = this.WeekInterval
            };

            if (this.DaysOfWeek != null)
            {
                recurrence.AdvancedSchedule = new AdvancedSchedule()
                {
                    WeekDays = this.DaysOfWeek.Select(day => day.ToString()).ToList()
                };
            }

            return recurrence;
        }

        private RecurrenceInfo GetMonthlyDayOfWeekRecurrence()
        {
            var dayOfWeek = this.DayOfWeek.HasValue ? this.DayOfWeek.ToString() : null;
            if ((!string.IsNullOrWhiteSpace(dayOfWeek) && this.DayOfWeekOccurrence == 0) || (string.IsNullOrWhiteSpace(dayOfWeek) && this.DayOfWeekOccurrence != 0))
            {
                throw new ArgumentException(Resources.MonthlyScheduleNeedsDayOfWeekAndOccurrence);
            }

            var recurrence = new RecurrenceInfo
            {
                Frequency = ScheduleFrequency.Month,
                Interval = this.MonthInterval
            };

            if (this.DayOfWeek != null && this.DayOfWeekOccurrence != 0)
            {
                recurrence.AdvancedSchedule = new AdvancedSchedule()
                {
                    MonthlyOccurrences = new AdvancedScheduleMonthlyOccurrence[]
                    {
                        new AdvancedScheduleMonthlyOccurrence()
                        {
                            Day = dayOfWeek,
                            Occurrence = Convert.ToInt32(this.DayOfWeekOccurrence)
                        }
                    }
                };
            }

            return recurrence;
        }

        private RecurrenceInfo GetMonthlyDaysOfMonthRecurrence()
        {
            var recurrence = new RecurrenceInfo
            {
                Frequency = ScheduleFrequency.Month,
                Interval = this.MonthInterval
            };

            if (this.DaysOfMonth != null)
            {
                recurrence.AdvancedSchedule = new AdvancedSchedule()
                {
                    MonthDays = this.DaysOfMonth.Select(v => Convert.ToInt32(v)).ToList()
                };
            }

            return recurrence;
        }

        /// <summary>
        /// Private class used to group recurrence information.
        /// </summary>
        private class RecurrenceInfo
        {
            public ScheduleFrequency? Frequency;
            public byte? Interval;
            public AdvancedSchedule AdvancedSchedule;
        }
    }
}