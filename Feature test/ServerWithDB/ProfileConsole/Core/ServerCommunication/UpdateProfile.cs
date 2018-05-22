﻿using System;
using System.Collections.Generic;
using System.Linq;
using ProfileConsole.Core.Domain;
using ProfileConsole.Core.ServerCommunication.Interfaces;
using ProfileConsole.Persistence;

namespace ProfileConsole.Core.ServerCommunication
{
    public class UpdateProfile //: IUpdateProfile
    {
        //private IUnitOfWork unitOfWork;
        private static UnitOfWork unitOfWork;

        public UpdateProfile() 
        {
            unitOfWork = new UnitOfWork(new ProfileContext());
        }

        public static void UpdateProfileInformation(string Username, string Description, List<string> Tagslist)
        {
            unitOfWork = new UnitOfWork(new ProfileContext());

            UserInformation person = null;

            try
            {
                person = unitOfWork.UserInformation.GetString(Username);
            }
            catch (Exception e)
            {

            }

            if (person.UserName == Username)
            {
                person.Description = Description;
                person.Tags = Tagslist;
                unitOfWork.Complete();
            }
                        
                 

                    

                

                
            
        }
    }
}
