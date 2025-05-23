﻿using Moq;
using NUnit.Framework;
using PeerTutoringSystem.Application.DTOs.Booking;
using PeerTutoringSystem.Application.Interfaces.Authentication;
using PeerTutoringSystem.Application.Services.Booking;
using PeerTutoringSystem.Domain.Entities.Booking;
using PeerTutoringSystem.Domain.Interfaces.Booking;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace PeerTutoringSystem.Tests.Application.Services.Advanced
{
    [TestFixture]
    public class BookingServiceAdvancedTests
    {
        [Test]
        public async Task CancelBooking_ShouldMakeSlotAvailableAgain()
        {
            // Arrange
            var fixture = new BookingServiceTestFixture();
            var bookingId = Guid.NewGuid();
            var availabilityId = Guid.NewGuid();

            var booking = new BookingSession
            {
                BookingId = bookingId,
                AvailabilityId = availabilityId,
                Status = BookingStatus.Pending
            };

            var availability = new TutorAvailability
            {
                AvailabilityId = availabilityId,
                IsBooked = true
            };

            fixture.MockBookingRepository
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            fixture.MockAvailabilityRepository
                .Setup(r => r.GetByIdAsync(availabilityId))
                .ReturnsAsync(availability);

            var updateDto = new UpdateBookingStatusDto { Status = "Cancelled" };

            // Act
            await fixture.BookingService.UpdateBookingStatusAsync(bookingId, updateDto);

            // Assert
            fixture.MockAvailabilityRepository.Verify(
                r => r.UpdateAsync(It.Is<TutorAvailability>(a => a.IsBooked == false)),
                Times.Once);
        }

        [Test]
        public void CompleteBookingInFuture_ShouldThrowValidationException()
        {
            // Arrange
            var fixture = new BookingServiceTestFixture();
            var bookingId = Guid.NewGuid();

            var booking = new BookingSession
            {
                BookingId = bookingId,
                StartTime = DateTime.UtcNow.AddHours(1),  // Future time
                EndTime = DateTime.UtcNow.AddHours(2),    // Future time
                Status = BookingStatus.Confirmed
            };

            fixture.MockBookingRepository
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            var updateDto = new UpdateBookingStatusDto { Status = "Completed" };

            // Act & Assert
            Assert.ThrowsAsync<ValidationException>(async () =>
                await fixture.BookingService.UpdateBookingStatusAsync(bookingId, updateDto));
        }

        [Test]
        public async Task GetUpcomingBookings_StudentRole_ReturnsOnlyStudentBookings()
        {
            // Arrange
            var fixture = new BookingServiceTestFixture();
            var studentId = Guid.NewGuid();

            var upcomingBookings = new List<BookingSession>
            {
                new BookingSession { StudentId = studentId, StartTime = DateTime.UtcNow.AddHours(1) }
            };

            fixture.MockBookingRepository
                .Setup(r => r.GetUpcomingBookingsByUserAsync(studentId, false)) // isTutor = false
                .ReturnsAsync(upcomingBookings);

            // Act
            var result = await fixture.BookingService.GetUpcomingBookingsAsync(studentId, false);

            // Assert
            Assert.That(result, Is.Not.Null);
            fixture.MockBookingRepository.Verify(
                r => r.GetUpcomingBookingsByUserAsync(studentId, false),
                Times.Once);
        }

        [Test]
        public async Task GetUpcomingBookings_TutorRole_ReturnsOnlyTutorBookings()
        {
            // Arrange
            var fixture = new BookingServiceTestFixture();
            var tutorId = Guid.NewGuid();

            var upcomingBookings = new List<BookingSession>
            {
                new BookingSession { TutorId = tutorId, StartTime = DateTime.UtcNow.AddHours(2) }
            };

            fixture.MockBookingRepository
                .Setup(r => r.GetUpcomingBookingsByUserAsync(tutorId, true)) // isTutor = true
                .ReturnsAsync(upcomingBookings);

            // Act
            var result = await fixture.BookingService.GetUpcomingBookingsAsync(tutorId, true);

            // Assert
            Assert.That(result, Is.Not.Null);
            fixture.MockBookingRepository.Verify(
                r => r.GetUpcomingBookingsByUserAsync(tutorId, true),
                Times.Once);
        }

        [Test]
        public async Task BookingWithNullTopic_ShouldUseDefaultTopic()
        {
            // Arrange
            var fixture = new BookingServiceTestFixture();
            var studentId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var availabilityId = Guid.NewGuid();

            var availability = new TutorAvailability
            {
                AvailabilityId = availabilityId,
                TutorId = tutorId,
                StartTime = DateTime.UtcNow.AddHours(3),
                EndTime = DateTime.UtcNow.AddHours(4),
                IsBooked = false
            };

            fixture.MockAvailabilityRepository
                .Setup(r => r.GetByIdAsync(availabilityId))
                .ReturnsAsync(availability);

            fixture.MockBookingRepository
                .Setup(r => r.IsSlotAvailableAsync(tutorId, availability.StartTime, availability.EndTime))
                .ReturnsAsync(true);

            var createBookingDto = new CreateBookingDto
            {
                TutorId = tutorId,
                AvailabilityId = availabilityId,
                Topic = null,  // null topic should use default
                Description = "Testing null topic handling"
            };

            // Act
            var result = await fixture.BookingService.CreateBookingAsync(studentId, createBookingDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Topic, Is.EqualTo("General tutoring session"));
        }

        // Test fixture to reduce boilerplate code
        private class BookingServiceTestFixture
        {
            public Mock<IBookingSessionRepository> MockBookingRepository { get; }
            public Mock<ITutorAvailabilityRepository> MockAvailabilityRepository { get; }
            public Mock<IUserService> MockUserService { get; }
            public BookingService BookingService { get; }

            public BookingServiceTestFixture()
            {
                MockBookingRepository = new Mock<IBookingSessionRepository>();
                MockAvailabilityRepository = new Mock<ITutorAvailabilityRepository>();
                MockUserService = new Mock<IUserService>();

                BookingService = new BookingService(
                    MockBookingRepository.Object,
                    MockAvailabilityRepository.Object,
                    MockUserService.Object);
            }
        }
    }
}