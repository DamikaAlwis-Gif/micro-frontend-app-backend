using AutoMapper;
using FaqService.Dal;
using FaqService.Domain.Dtos;
using FaqService.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FaqService.Service.EventProcessing
{
    public class EventProcessor : IEventProcessor
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMapper _mapper;

        public EventProcessor(IServiceScopeFactory serviceScopeFactory, IMapper mapper)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _mapper = mapper;
        }

        public void ProcessEvent(string message)
        {
            var eventType = DetermineEvent(message);

            if(eventType.Item1 == EventType.UserPublished) 
            {
                switch(eventType.Item2.MessageType) 
                {
                    case "Add":
                        AddUser(message);
                        return;

                    case "Delete":
                        DeleteUser(message); 
                        return;

                    case "Update":
                        UpdateUser(message);
                        return;

                    default:
                        return;
                }
            }
        }


        private void UpdateUser(string userPublishedMessage)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IFaqRepo>();

                var userPublishedDto = JsonSerializer.Deserialize<UserPublishedDto>(userPublishedMessage);

                try
                {
                    var user = _mapper.Map<User>(userPublishedDto);
                    if (repo.UserExists(user.Id))
                    {
                        repo.UpdateUser(user);
                        repo.SaveChanges();
                        Console.WriteLine("---> User Updated!");
                    }
                    else
                    {
                        Console.WriteLine("---> User not exists!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"---> Could not update User: {ex.Message}");
                }
            }
        }


        private void DeleteUser(string userPublishedMessage)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IFaqRepo>();

                var userPublishedDto = JsonSerializer.Deserialize<UserPublishedDto>(userPublishedMessage);

                try
                {
                    var user = _mapper.Map<User>(userPublishedDto);
                    if (repo.UserExists(user.Id))
                    {
                        repo.DeleteUser(user);
                        repo.SaveChanges();
                        Console.WriteLine("---> User Deleted!");
                    }
                    else
                    {
                        Console.WriteLine("---> User not exists!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"---> Could not delete User: {ex.Message}");
                }
            }
        }

        private void AddUser(string userPublishedMessage)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IFaqRepo>();

                var userPublishedDto = JsonSerializer.Deserialize<UserPublishedDto>(userPublishedMessage);

                try
                {
                    var user = _mapper.Map<User>(userPublishedDto);
                    if (!repo.UserExists(user.Id))
                    {
                        repo.CreateUser(user);
                        repo.SaveChanges();
                        Console.WriteLine("---> User Added!");
                    }
                    else
                    {
                        Console.WriteLine("---> User already exists!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"---> Could not add User to DB: {ex.Message}");
                }
            }
        }

        private (EventType, GenericEventDto) DetermineEvent(string notificationMessage)
        {
            Console.WriteLine("---> Determining Event...");

            var message = JsonSerializer.Deserialize<GenericEventDto>(notificationMessage);

            switch (message.EventType)
            {
                case "UserToFaqMessage":
                    Console.WriteLine("---> User microservice published event detected");
                    return (EventType.UserPublished, message);

                default:
                    Console.WriteLine("---> Could not determine the event type");
                    return (EventType.Undetermined, null);
            }
        }
    }

    enum EventType
    {
        UserPublished,
        Undetermined
    }
}
