using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using JetBrains.Annotations;

namespace Kohctpyktop.Avalonia.ViewModels
{
    public interface IMatrix<T>
    {
        uint Width { get; }
        uint Height { get; }
        T this[uint x, uint y] { get; set; }
    }
    
    public class Matrix<T> : IMatrix<T>
    {
        private readonly T[,] _backingMatrix;

        private Matrix(T[,] backingMatrix, uint width, uint height)
        {
            Width = width;
            Height = height;
            _backingMatrix = backingMatrix;
        }
        
        public Matrix(uint width, uint height) 
            : this(new T[width, height], width, height) {}
        
        // TODO: copy constructor
        public Matrix(T[,] backingMatrix, T outOfRangeDefault)
            : this(backingMatrix ?? throw new ArgumentNullException(nameof(backingMatrix)),
                (uint) backingMatrix.GetLength(0),
                (uint) backingMatrix.GetLength(1)) {}

        public uint Width { get; }
        public uint Height { get; }

        public T this[uint x, uint y]
        {
            get => _backingMatrix[x, y];
            set => _backingMatrix[x, y] = value;
        }
    }
    
    public class SafeMatrix<T> : IMatrix<T>
    {
        private readonly IMatrix<T> _backingMatrix;
        private readonly T _outOfRangeDefault;

        private SafeMatrix(IMatrix<T> backingMatrix, uint width, uint height, T outOfRangeDefault)
        {
            Width = width;
            Height = height;
            _backingMatrix = backingMatrix;
            _outOfRangeDefault = outOfRangeDefault;
        }
        
        public SafeMatrix(uint width, uint height, T outOfRangeDefault) 
            : this(new Matrix<T>(width, height), width, height, outOfRangeDefault) {}
        
        // TODO: copy constructor
        public SafeMatrix(IMatrix<T> backingMatrix, T outOfRangeDefault)
            : this(backingMatrix ?? throw new ArgumentNullException(nameof(backingMatrix)),
                backingMatrix.Width,
                backingMatrix.Height,
                outOfRangeDefault) {}

        public uint Width { get; }
        public uint Height { get; }

        public T this[uint x, uint y]
        {
            get => x >= Width || y >= Height ? _outOfRangeDefault : _backingMatrix[x, y];
            set
            {
                if (x < Width && y < Height) _backingMatrix[x, y] = value;
            }
        }
    }

    public struct Position
    {
        public Position(uint x, uint y)
        {
            X = x;
            Y = y;
        }

        public uint X { get; }
        public uint Y { get; }
    }

    public class NotifyingMatrix<T> : IMatrix<T>
    {
        private readonly IMatrix<T> _matrix;

        public NotifyingMatrix(IMatrix<T> matrix)
        {
            _matrix = matrix ?? throw new ArgumentNullException(nameof(matrix));
            Width = matrix.Width;
            Height = matrix.Height;
        }

        public event EventHandler<Position> CellChangedAtPosition;
        
        public uint Width { get; }
        public uint Height { get; }

        public T this[uint x, uint y]
        {
            get => _matrix[x, y];
            set
            {
                _matrix[x, y] = value;
                CellChangedAtPosition?.Invoke(this, new Position(x, y));
            }
        }
    }

    public class MatrixFlatView<T> : IReadOnlyList<T>
    {
        private readonly IMatrix<T> _matrix;
        private readonly uint _width, _height;

        public MatrixFlatView(IMatrix<T> matrix)
        {
            _matrix = matrix;
            _width = matrix.Width;
            _height = matrix.Height;
            
            Count = (int) (_width * _height);
        }

        private IEnumerable<T> Iterate()
        {
            for (var i = 0u; i < _width; i++)
            for (var j = 0u; j < _height; j++)
                yield return _matrix[i, j];
        }

        public IEnumerator<T> GetEnumerator() => Iterate().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count { get; }

        public T this[int index] => index < 0
            ? throw new ArgumentOutOfRangeException(nameof(index))
            : _matrix[(uint) (index % _width), (uint) (index / _width)];
    }

    public struct Cell
    {
        public Cell(bool hasMetal)
        {
            HasMetal = hasMetal;
        }

        public bool HasMetal { get; }
    }

    public struct Link
    {
        public Link(bool hasMetalLink)
        {
            HasMetalLink = hasMetalLink;
        }

        public bool HasMetalLink { get; }
    }

    public class Field
    {
        public Field(uint width, uint height)
        {
            Cells = new Matrix<Cell>(width, height);
            LeftToRightLinks = new SafeMatrix<Link>(new Matrix<Link>(width - 1, height - 1), default);
            TopToBottomLinks = new SafeMatrix<Link>(new Matrix<Link>(width - 1, height - 1), default);
        }
        
        public Matrix<Cell> Cells { get; }
        
        public SafeMatrix<Link> LeftToRightLinks { get; }
        public SafeMatrix<Link> TopToBottomLinks { get; }
    }

    public class CellViewModel : INotifyPropertyChanged
    {
        private readonly Field _field;
        private readonly uint _x;
        private readonly uint _y;

        public CellViewModel(Field field, uint x, uint y)
        {
            _field = field;
            _x = x;
            _y = y;

            Refresh();
        }

        public void Refresh()
        {
            HasMetal = _field.Cells[_x, _y].HasMetal;
            
            HasLeftMetalLink = _field.LeftToRightLinks[_x - 1, _y].HasMetalLink;
            HasTopMetalLink = _field.TopToBottomLinks[_x, _y - 1].HasMetalLink;
            HasRightMetalLink = _field.LeftToRightLinks[_x, _y].HasMetalLink;
            HasBottomMetalLink = _field.TopToBottomLinks[_x, _y].HasMetalLink;
            
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        public double DisplayX => _x * 16;
        public double DisplayY => _y * 16;
        public ISolidColorBrush DisplayColor => HasMetal ? Brushes.Black : Brushes.Wheat;
        
        public bool HasMetal { get; private set; }
        
        public bool HasLeftMetalLink { get; private set; }
        public bool HasTopMetalLink { get; private set; }
        public bool HasRightMetalLink { get; private set; }
        public bool HasBottomMetalLink { get; private set; }
        
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class CellFieldViewModel
    {
        private readonly Matrix<CellViewModel> _cellMatrix;

        public CellFieldViewModel(Field field)
        { 
            _cellMatrix = new Matrix<CellViewModel>(field.Cells.Width, field.Cells.Height);
            
            for (var i = 0u; i < _cellMatrix.Width; i++)
            for (var j = 0u; j < _cellMatrix.Height; j++)
            {
                _cellMatrix[i, j] = new CellViewModel(field, i, j);
            }
            
            Cells = new MatrixFlatView<CellViewModel>(_cellMatrix);
        }
        
        public MatrixFlatView<CellViewModel> Cells { get; }
    }
}