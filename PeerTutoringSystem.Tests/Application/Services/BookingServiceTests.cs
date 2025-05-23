﻿using Moq;
using NUnit.Framework;
using PeerTutoringSystem.Application.DTOs.Booking;
using PeerTutoringSystem.Application.Interfaces.Authentication;
using PeerTutoringSystem.Application.Services.Booking;
using PeerTutoringSystem.Domain.Entities.Authentication;
using PeerTutoringSystem.Domain.Entities.Booking;
using PeerTutoringSystem.Domain.Interfaces.Booking;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PeerTutoringSystem.Tests.Application.Services
{
    [TestFixture]
    public class BookingServiceTests
    {
        private Mock<IBookingSessionRepository> _mockBookingRepository;
        private Mock<ITutorAvailabilityRepository> _mockAvailabilityRepository;
        private Mock<IUserService> _mockUserService;
        private BookingService _bookingService;

        private Guid _studentId = Guid.NewGuid();
        private Guid _tutorId = Guid.NewGuid();
        private Guid _availabilityId = Guid.NewGuid();
        private Guid _bookingId = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            _mockBookingRepository = new Mock<IBookingSessionRepository>();
            _mockAvailabilityRepository = new Mock<ITutorAvailabilityRepository>();
            _mockUserService = new Mock<IUserService>();

            _bookingService = new BookingService(
                _mockBookingRepository.Object,
                _mockAvailabilityRepository.Object,
                _mockUserService.Object);
        }

        [Test]
        public async Task CreateBookingAsync_ValidBooking_ReturnsBookingDto()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddHours(1);
            var endTime = startTime.AddHours(1);

            var availability = new TutorAvailability
            {
                AvailabilityId = _availabilityId,
                TutorId = _tutorId,
                StartTime = startTime,
                EndTime = endTime,
                IsBooked = false
            };

            var createBookingDto = new CreateBookingDto
            {
                TutorId = _tutorId,
                AvailabilityId = _availabilityId,
                Topic = "Math Help",
                Description = "Need help with calculus"
            };

            _mockAvailabilityRepository
                .Setup(r => r.GetByIdAsync(_availabilityId))
                .ReturnsAsync(availability);

            _mockBookingRepository
                .Setup(r => r.IsSlotAvailableAsync(_tutorId, startTime, endTime))
                .ReturnsAsync(true);

            // Act
            var result = await _bookingService.CreateBookingAsync(_studentId, createBookingDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TutorId, Is.EqualTo(_tutorId));
            Assert.That(result.StudentId, Is.EqualTo(_studentId));
            Assert.That(result.Topic, Is.EqualTo("Math Help"));
            Assert.That(result.Status, Is.EqualTo("Pending"));

            _mockBookingRepository.Verify(r => r.AddAsync(It.IsAny<BookingSession>()), Times.Once);
            _mockAvailabilityRepository.Verify(r => r.UpdateAsync(It.IsAny<TutorAvailability>()), Times.Once);
        }

        [Test]
        public void CreateBookingAsync_AlreadyBookedSlot_ThrowsException()
        {
            // Arrange
            var availability = new TutorAvailability
            {
                AvailabilityId = _availabilityId,
                TutorId = _tutorId,
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(2),
                IsBooked = true
            };

            var createBookingDto = new CreateBookingDto
            {
                TutorId = _tutorId,
                AvailabilityId = _availabilityId
            };

            _mockAvailabilityRepository
                .Setup(r => r.GetByIdAsync(_availabilityId))
                .ReturnsAsync(availability);

            // Act & Assert
            Assert.ThrowsAsync<ValidationException>(async () =>
                await _bookingService.CreateBookingAsync(_studentId, createBookingDto));
        }

        [Test]
        public void CreateBookingAsync_TutorIdMismatch_ThrowsException()
        {
            // Arrange
            var differentTutorId = Guid.NewGuid();

            var availability = new TutorAvailability
            {
                AvailabilityId = _availabilityId,
                TutorId = _tutorId,
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(2),
                IsBooked = false
            };

            var createBookingDto = new CreateBookingDto
            {
                TutorId = differentTutorId,  // Different tutor ID
                AvailabilityId = _availabilityId
            };

            _mockAvailabilityRepository
                .Setup(r => r.GetByIdAsync(_availabilityId))
                .ReturnsAsync(availability);

            // Act & Assert
            Assert.ThrowsAsync<ValidationException>(async () =>
                await _bookingService.CreateBookingAsync(_studentId, createBookingDto));
        }

        [Test]
        public async Task GetBookingByIdAsync_ExistingBooking_ReturnsBookingDto()
        {
            // Arrange
            var booking = new BookingSession
            {
                BookingId = _bookingId,
                StudentId = _studentId,
                TutorId = _tutorId,
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
                Topic = "Physics Help",
                Description = "Need help with mechanics",
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var student = new User { FirstName = "John", LastName = "Student" };
            var tutor = new User { FirstName = "Jane", LastName = "Tutor" };

            _mockBookingRepository
                .Setup(r => r.GetByIdAsync(_bookingId))
                .ReturnsAsync(booking);

            _mockUserService
                .Setup(s => s.GetUserByIdAsync(_studentId.ToString()))
                .ReturnsAsync(student);

            _mockUserService
                .Setup(s => s.GetUserByIdAsync(_tutorId.ToString()))
                .ReturnsAsync(tutor);

            // Act
            var result = await _bookingService.GetBookingByIdAsync(_bookingId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.BookingId, Is.EqualTo(_bookingId));
            Assert.That(result.StudentName, Is.EqualTo("John Student"));
            Assert.That(result.TutorName, Is.EqualTo("Jane Tutor"));
        }

        [Test]
        public async Task GetBookingsByStudentAsync_ReturnsStudentBookings()
        {
            // Arrange
            var bookings = new List<BookingSession>
            {
                new BookingSession
                {
                    BookingId = _bookingId,
                    StudentId = _studentId,
                    TutorId = _tutorId,
                    StartTime = DateTime.UtcNow.AddHours(2),
                    EndTime = DateTime.UtcNow.AddHours(3),
                    Status = BookingStatus.Pending
                }
            };

            _mockBookingRepository
                .Setup(r => r.GetByStudentIdAsync(_studentId))
                .ReturnsAsync(bookings);

            // Act
            var result = await _bookingService.GetBookingsByStudentAsync(_studentId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task UpdateBookingStatusAsync_ValidStatusChange_ReturnsUpdatedBooking()
        {
            // Arrange
            var booking = new BookingSession
            {
                BookingId = _bookingId,
                StudentId = _studentId,
                TutorId = _tutorId,
                AvailabilityId = _availabilityId,
                StartTime = DateTime.UtcNow.AddHours(-2),
                EndTime = DateTime.UtcNow.AddHours(-1),
                Status = BookingStatus.Pending
            };

            var updateDto = new UpdateBookingStatusDto { Status = "Completed" };

            _mockBookingRepository
                .Setup(r => r.GetByIdAsync(_bookingId))
                .ReturnsAsync(booking);

            // Act
            var result = await _bookingService.UpdateBookingStatusAsync(_bookingId, updateDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo("Completed"));
            _mockBookingRepository.Verify(r => r.UpdateAsync(It.Is<BookingSession>(b => b.Status == BookingStatus.Completed)), Times.Once);
        }

        [Test]
        public void UpdateBookingStatusAsync_InvalidStatus_ThrowsException()
        {
            // Arrange
            var booking = new BookingSession
            {
                BookingId = _bookingId,
                Status = BookingStatus.Pending
            };

            var updateDto = new UpdateBookingStatusDto { Status = "InvalidStatus" };

            _mockBookingRepository
                .Setup(r => r.GetByIdAsync(_bookingId))
                .ReturnsAsync(booking);

            // Act & Assert
            Assert.ThrowsAsync<ValidationException>(async () =>
                await _bookingService.UpdateBookingStatusAsync(_bookingId, updateDto));
        }

        [Test]
        public void UpdateBookingStatusAsync_CompleteToCancel_ThrowsException()
        {
            // Arrange
            var booking = new BookingSession
            {
                BookingId = _bookingId,
                Status = BookingStatus.Completed
            };

            var updateDto = new UpdateBookingStatusDto { Status = "Cancelled" };

            _mockBookingRepository
                .Setup(r => r.GetByIdAsync(_bookingId))
                .ReturnsAsync(booking);

            // Act & Assert
            Assert.ThrowsAsync<ValidationException>(async () =>
                await _bookingService.UpdateBookingStatusAsync(_bookingId, updateDto));
        }
    }
}