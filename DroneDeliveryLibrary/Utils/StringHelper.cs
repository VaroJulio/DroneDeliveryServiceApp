﻿namespace DroneDeliveryLibrary.Utils
{
    public static class StringHelper
    {
        public static string Remove(this string theString, string[] stringListToRemove)
        {
            if (stringListToRemove.Length > 0)
            {
                foreach (var character in stringListToRemove)
                    theString = theString.Replace(character, null);
            }

            return theString;
        }
    }
}
