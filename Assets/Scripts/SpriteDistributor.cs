using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class SpriteDistributor : MonoBehaviour
{
    [SerializeField] private List<Sprite> sprites;

    private Random _random;
    private Image[] _images;

    void Start()
    {
        _random = new Random();
        _images = gameObject.GetComponentsInChildren<Image>();
         foreach (var img in _images)
         {
             img.alphaHitTestMinimumThreshold = .5f;
         }

        InvokeRepeating(nameof(UpdateImages), 0f, 5f);
    }
    
    
    private void UpdateImages()
    {
        var randomSprites = sprites.OrderBy(x => _random.Next()).Take(_images.Length).ToArray();
        for (var i = 0; i < _images.Length; i++)
        {
            _images[i].sprite = randomSprites[i];
        }
    }
}